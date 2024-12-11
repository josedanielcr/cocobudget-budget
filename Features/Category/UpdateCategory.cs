using System.Data;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Category.Requests;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Category;

public static class UpdateCategory
{
    public class Command : IRequest<Result<CategoryResponse>>
    {
        public Guid Id { get; set; }
        public CategoryType CategoryType { get; set; }
        public DateTime? FinalDate { get; set; }
        public decimal GeneralTargetAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public required string Name { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.GeneralTargetAmount).NotEmpty();
            RuleFor(x => x.CategoryType).IsInEnum();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<CategoryResponse>>
    {
        public async Task<Result<CategoryResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<CategoryResponse>(new Error("UpdateCategory.Validation",
                    validationResult.ToString()));
            }

            var category = await dbContext.Categories
                .Where(x => x.Id == request.Id)
                .Include(x => x.GeneralCategory)
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
            {
                return Result.Failure<CategoryResponse>(new Error("UpdateCategory.CategoryNotFound",
                    "Category not found"));
            }

            try
            {
                var updatedCategory = await UpdateCategory(request, category, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new CategoryResponse
                {
                    Id = updatedCategory.Id,
                    GeneralId = updatedCategory.GeneralId,
                    Name = updatedCategory.Name,
                    FolderId = updatedCategory.FolderId,
                    GeneralCategory = updatedCategory.GeneralCategory,
                    GeneralCategoryId = updatedCategory.GeneralCategoryId,
                    TargetAmount = updatedCategory.TargetAmount,
                    BudgetAmount = updatedCategory.BudgetAmount,
                    AmountSpent = updatedCategory.AmountSpent,
                    AmountRemaining = updatedCategory.AmountRemaining,
                    CreatedOn = updatedCategory.CreatedOn,
                    ModifiedOn = updatedCategory.ModifiedOn,
                    IsActive = updatedCategory.IsActive
                };
            }
            catch (DataException e)
            {
                return Result.Failure<CategoryResponse>(new Error("UpdateCategory.DataException", e.Message));
            }
        }

        private async Task<Entities.Category> UpdateCategory(Command request, Entities.Category category, CancellationToken cancellationToken)
        {
            return request.CategoryType == CategoryType.Fixed 
                ? await UpdateFixedCategory(request, category) 
                : await UpdateCustomCategory(request, category, cancellationToken);
        }

        private async Task<Entities.Category> UpdateCustomCategory(Command request, Entities.Category category, CancellationToken cancellationToken)
        {
            category.Name = request.Name;
            category.GeneralCategory.TargetAmount = request.GeneralTargetAmount;

            var folder = await FolderExtensions.GetFolderAsync(dbContext, category.FolderId, cancellationToken);
            var period = await PeriodExtensions.GetUserActivePeriodAsync(dbContext, folder.UserId, cancellationToken);
            
            if (period == null)
            {
                throw new DataException("No active period found for the user");
            }

            if (category.GeneralCategory.FinalDate == null)
            {
                category.TargetAmount = request.TargetAmount;
                category.AmountRemaining = (request.TargetAmount - category.AmountSpent);
            }
            else
            {
                var now = DateTime.Now;
                var difference = request.FinalDate!.Value - now;
                var totalDays = (int)difference.TotalDays;
                var amountOfPeriods = totalDays / period.DayLength;
                category.TargetAmount = Math.Round(category.GeneralCategory.TargetAmount / amountOfPeriods,2);
                category.AmountRemaining = (category.TargetAmount - category.AmountSpent);
            }
            dbContext.Categories.Update(category);
            return category;
        }

        private async Task<Entities.Category> UpdateFixedCategory(Command request, Entities.Category category)
        {
            category.Name = request.Name;
            category.GeneralCategory.TargetAmount = request.GeneralTargetAmount;
            category.TargetAmount = request.GeneralTargetAmount;
            category.AmountRemaining = (request.GeneralTargetAmount - category.AmountSpent);
            dbContext.Categories.Update(category);
            return category;
        }
    }
}

public class UpdateCategoryEndpoint : ICarterModule
{
    private const string RouteTag = "Categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/category/{id:guid}", async (Guid id, UpdateCategoryRequest request, ISender sender) =>
        {
            var command = new UpdateCategory.Command
            {
                Id = request.Id,
                CategoryType = request.CategoryType,
                FinalDate = request.FinalDate,
                GeneralTargetAmount = request.GeneralTargetAmount,
                TargetAmount = request.TargetAmount,
                Name = request.Name
            };
            var result = await sender.Send(command);
            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}

