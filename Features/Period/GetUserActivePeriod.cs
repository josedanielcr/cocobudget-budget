using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Period.Responses;
using web_api.Database;
using web_api.Extensions;
using web_api.Migrations;
using web_api.Shared;

namespace web_api.Features.Period;

public static class GetUserActivePeriod
{
    public class Query : IRequest<Result<PeriodResponse>>
    {
        public Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Hanlder : IRequestHandler<Query,Result<PeriodResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Query> _validator;

        public Hanlder(ApplicationDbContext dbContext, IValidator<Query> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }
        
        public async Task<Result<PeriodResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request,cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PeriodResponse>(new Error("GetUserActivePeriod.Validation",
                    validationResult.ToString()));
            }

            var period = await PeriodExtensions.GetUserActivePeriodAsync(_dbContext, request.UserId, cancellationToken);
            
            if (period == null)
            {
                return Result.Failure<PeriodResponse>(new Error("GetUserActivePeriod.Period",
                    "No active period found for the user"));
            }
            
            return Result.Success(new PeriodResponse(
                period.Id,
                period.StartDate,
                period.EndDate,
                period.Length,
                period.DayLength,
                period.UserId,
                period.AmountSpent,
                period.BudgetAmount
                ));
        }
    }
}

public class GetUserActivePeriodEndPoint : ICarterModule
{
    private const string RouteTag = "Periods";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/period/active/{userId}", async (Guid userId, ISender sender) =>
        {
            var query = new GetUserActivePeriod.Query { UserId = userId };
            var result = await sender.Send(query);
            return result.IsFailure ? Results.NotFound(result.Error) : Results.Ok(result);
            
        }).WithTags(RouteTag);
    }
}