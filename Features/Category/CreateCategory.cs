using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Category.Requests;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Enums;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Category;

public static class CreateCategory
{
    public class Command : IRequest<Result<CategoryResponse>>
    {
        public required Guid UserId { get; set; }
        public required Guid FolderId { get; set; }
        public CategoryType CategoryType { get; set; } = CategoryType.Fixed;
        public DateTime? FinalDate { get; set; }
        public required string Currency { get; set; }
        public required decimal GeneralTargetAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public required string Name { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.FolderId).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty();
            RuleFor(x => x.GeneralTargetAmount).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
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
                return Result.Failure<CategoryResponse>(new Error("CreateCategory.Validation",
                    validationResult.ToString()));
            }

            var folder = await dbContext.GetFolderAsync(request.FolderId, cancellationToken);
            if (folder.UserId != request.UserId)
            {
                return Result.Failure<CategoryResponse>(new Error("CreateCategory.FolderNotFound",
                    "Folder not found"));
            }
            
            GeneralCategory generalCategory = await this.CreateGeneralCategory(dbContext,request, folder);
            var activePeriod = await dbContext.GetUserActivePeriodAsync(request.UserId,cancellationToken);
            
            if (activePeriod == null)
            {
                return Result.Failure<CategoryResponse>(new Error("CreateCategory.ActivePeriodNotFound",
                    "Active period not found"));
            }
            
            Entities.Category result = await this.CreateCategory(dbContext, request, generalCategory, activePeriod);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return new CategoryResponse
            {
                Id = result.Id,
                GeneralId = result.GeneralId,
                Name = result.Name,
                FolderId = result.FolderId,
                GeneralCategory = result.GeneralCategory,
                GeneralCategoryId = result.GeneralCategoryId,
                TargetAmount = result.TargetAmount,
                BudgetAmount = result.BudgetAmount,
                AmountSpent = result.AmountSpent,
                AmountRemaining = result.AmountRemaining,
                CreatedOn = result.CreatedOn,
                ModifiedOn = result.ModifiedOn,
                IsActive = result.IsActive
            };
        }

        private async Task<GeneralCategory> CreateGeneralCategory(ApplicationDbContext dbContext, Command request, Entities.Folder folder)
        {
            var generalCategory = new Entities.GeneralCategory
            {
                Currency = request.Currency,
                TargetAmount = request.GeneralTargetAmount,
                FinalDate = request.FinalDate,
                CategoryType = request.CategoryType,
                UserId = request.UserId
            };

            await dbContext.GeneralCategories.AddAsync(generalCategory);
            return generalCategory;
        }
        
        private async Task<Entities.Category> CreateCategory(ApplicationDbContext applicationDbContext, Command request,
            GeneralCategory generalCategory, Entities.Period activePeriod)
        {
            var categoryTargetAmount = this.CalculateTargetAmount(request, generalCategory, activePeriod);
            var category = new Entities.Category
            {
                GeneralId = Guid.NewGuid(),
                Name = request.Name,
                FolderId = request.FolderId,
                GeneralCategory = generalCategory,
                GeneralCategoryId = generalCategory.Id,
                TargetAmount = categoryTargetAmount,
                BudgetAmount = 0,
                AmountSpent = 0,
                AmountRemaining = categoryTargetAmount
            };
            await applicationDbContext.Categories.AddAsync(category);
            return category;
        }

        private decimal CalculateTargetAmount(Command request, GeneralCategory generalCategory, Entities.Period activePeriod)
        {
            if (generalCategory.CategoryType == CategoryType.Fixed) return generalCategory.TargetAmount;
            if (generalCategory.FinalDate == null) return request.TargetAmount;
            var now = DateTime.Now;
            var difference = request.FinalDate!.Value - now;
            var totalDays = (int)difference.TotalDays;
            var amountOfPeriods = totalDays / activePeriod.DayLength;
            return Math.Round(generalCategory.TargetAmount / amountOfPeriods,2);
        }
    }
}

public class CreateCategoryEndpoint : ICarterModule
{
    private const string RouteTag = "Categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/category", async (CreateCategoryRequest request, ISender sender) =>
        {
            var command = new CreateCategory.Command
            {
                Name = request.Name,
                Currency = request.Currency,
                GeneralTargetAmount = request.GeneralTargetAmount,
                TargetAmount = request.TargetAmount,
                FinalDate = request.FinalDate,
                UserId = request.UserId,
                FolderId = request.FolderId,
                CategoryType = request.CategoryType
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
