using Carter;
using MediatR;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Category;

public static class DeleteCategory
{
    public class Command : IRequest<Result>
    {
        public Guid Id { get; set; }
    }

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = await _dbContext.Categories.FindAsync(request.Id);
            if (category == null)
            {
                return Result.Failure(new Error("DeleteCategory.NotFound", "Category not found"));
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
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
