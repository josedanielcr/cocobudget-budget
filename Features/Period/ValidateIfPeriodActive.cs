using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Period.Requests;
using web_api.Contracts.Period.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Period;

public static class ValidateIfPeriodActive
{
    public class Query : IRequest<Result<PeriodResponse>>
    {
        public required Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Query, Result<PeriodResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Query> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Query> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<PeriodResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PeriodResponse>(new Error("ValidatePeriodActive.Validation", validationResult.ToString()));
            }

            var period = await _dbContext.Periods
                .Where(p => p.UserId == request.UserId)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (period == null)
            {
                return Result.Failure<PeriodResponse>(new Error("ValidatePeriodActive.NotFound", "No se encontró un periodo activo para este usuario."));
            }

            if (DateTime.Now > period.EndDate)
            {
                return Result.Failure<PeriodResponse>(new Error("ValidatePeriodActive.Expired", "El periodo actual está vencido. Por favor, crea un nuevo periodo."));
            }

            var response = new PeriodResponse(
                period.Id,
                period.StartDate,
                period.EndDate,
                period.Length,
                period.DayLength,
                period.UserId,
                period.AmountSpent,
                period.BudgetAmount
            );

            return Result.Success(response);
        }
    }
}

public class ValidateIfPeriodActiveEndpoint : ICarterModule
{
    private const string RouteTag = "Periods";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/period/validateIfPeriodActive", async (Guid userId, ISender sender) =>
        {
            var query = new ValidateIfPeriodActive.Query
            {
                UserId = userId
            };

            var result = await sender.Send(query);

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        }).WithTags(RouteTag);
    }
}
