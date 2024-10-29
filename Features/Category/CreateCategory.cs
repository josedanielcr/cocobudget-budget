using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Category.Requests;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Category;

public static class CreateCategory
{
    public class Command : IRequest<Result<CategoryResponse>>
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
        public Guid FolderId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Icon).NotEmpty();
            RuleFor(x => x.ColorHex).NotEmpty();
            RuleFor(x => x.BudgetAmount).GreaterThanOrEqualTo(0); ;
            RuleFor(x => x.FolderId).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<CategoryResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<CategoryResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return Result.Failure<CategoryResponse>(new Error("CreateCategory.Validation",
                    validationResult.ToString()));
            }

            var category = new Entities.Category
            {
                Name = request.Name,
                Icon = request.Icon,
                ColorHex = request.ColorHex,
                BudgetAmount = request.BudgetAmount,
                FolderId = request.FolderId
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

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
                Icon = request.Icon,
                ColorHex = request.ColorHex,
                BudgetAmount = request.BudgetAmount,
                FolderId = request.FolderId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
