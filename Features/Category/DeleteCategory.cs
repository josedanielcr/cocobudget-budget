using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Category;

public static class DeleteCategory
{
    public class Command : IRequest<Result<CategoryResponse>>
    {
        public Guid Id { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<CategoryResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<CategoryResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = await _dbContext.Categories.Where(f => f.Id == request.Id)
                .Include(f => f.GeneralCategory)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (category == null)
            {
                return Result.Failure<CategoryResponse>(new Error("DeleteCategory.NotFound", "Category not found"));
            }
            
            category.IsActive = false;
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new CategoryResponse
            {
                Id = category.Id,
                GeneralId = category.GeneralId,
                Name = category.Name,
                FolderId = category.FolderId,
                GeneralCategory = category.GeneralCategory,
                GeneralCategoryId = category.GeneralCategoryId,
                TargetAmount = category.TargetAmount,
                BudgetAmount = category.BudgetAmount,
                AmountSpent = category.AmountSpent,
                AmountRemaining = category.AmountRemaining,
                CreatedOn = category.CreatedOn,
                ModifiedOn = category.ModifiedOn,
                IsActive = category.IsActive
            };
        }
    }
}

public class DeleteCategoryEndpoint : ICarterModule
{
    private const string RouteTag = "Categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/category/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCategory.Command { Id = id });
            return result.IsFailure
                ? Results.NotFound(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
