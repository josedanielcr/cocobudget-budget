using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Category.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Category;

public static class UpdateCategory
{
    public class Command : IRequest<Result<CategoryResponse>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Icon).NotEmpty();
            RuleFor(x => x.ColorHex).NotEmpty();
            RuleFor(x => x.BudgetAmount).GreaterThanOrEqualTo(0);
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
                return Result.Failure<CategoryResponse>(new Error("UpdateCategory.Validation",
                    validationResult.ToString()));
            }

            var category = await _dbContext.Categories.FindAsync(request.Id);
            if (category == null)
            {
                return Result.Failure<CategoryResponse>(new Error("UpdateCategory.NotFound", "Category not found"));
            }

            /*category.Name = request.Name;
            category.Icon = request.Icon;
            category.ColorHex = request.ColorHex;
            category.BudgetAmount = request.BudgetAmount;

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
            );*/
            return null;
        }
    }
}

public class UpdateCategoryEndpoint : ICarterModule
{
    private const string RouteTag = "Categories";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/category/{id:guid}", async (Guid id, UpdateCategory.Command command, ISender sender) =>
        {
            command.Id = id;
            var result = await sender.Send(command);
            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}

