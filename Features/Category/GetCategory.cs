using Carter;
using MediatR;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Category;

public static class GetCategory
{
    public class Query : IRequest<Result<CategoryResponse>>
    {
        public Guid Id { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Query, Result<CategoryResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<CategoryResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var category = await _dbContext.Categories.FindAsync(request.Id);
            if (category == null)
            {
                return Result.Failure<CategoryResponse>(new Error("GetCategory.NotFound", "Category not found"));
            }

            return new CategoryResponse(
                category.Id,
                category.Name,
                category.Icon,
                category.ColorHex,
                category.BudgetAmount,
                category.AmountSpent,
                category.FolderId,
                category.IsActive,
                category.CreatedOn,
                category.ModifiedOn,
                category.UserId
            );
        }
    }
}

public class GetCategoryEndpoint : ICarterModule
{
    private const string RouteTag = "Categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/category/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCategory.Query { Id = id });
            return result.IsFailure
                ? Results.NotFound(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
