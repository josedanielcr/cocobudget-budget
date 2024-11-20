using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Period.Requests;
using web_api.Contracts.Period.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Period;

public static class CreatePeriod
{
    public class Command : IRequest<Result<PeriodResponse>>
    {
        public required DateTime StartDate { get; set; }
        public required PeriodLength Length { get; set; }
        public int DayLength { get; set; }
        public required Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.StartDate)
                .Must((command, startDate) => BeValidStartDate(startDate, command.Length, command.DayLength))
                .NotEmpty();
            RuleFor(x => x.Length).NotEmpty();
            RuleFor(x => x.DayLength).Must((command, dayLength) => BeValidDayLength(dayLength, command.Length))
                .NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
        
        //if the period is custom then the day length must be greater than 0
        private bool BeValidDayLength(int dayLength, PeriodLength commandLength)
        {
            return commandLength != PeriodLength.Custom || dayLength > 0;
        }

        // the current date needs to be less than the start date plus the desired period length
        private bool BeValidStartDate(DateTime startDate, PeriodLength length, int dayLength)
        {
            var numberOfDays = PeriodExtensions.GetNumberOfDays(startDate, dayLength, length);
            return DateTime.Now <= startDate.AddDays(numberOfDays);
        }
    }

    internal sealed class Hanlder : IRequestHandler<Command, Result<PeriodResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Hanlder(ApplicationDbContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }
        
        public async Task<Result<PeriodResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request,cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PeriodResponse>(new Error("CreatePeriod.Validation",
                    validationResult.ToString()));
            }
            
            var period = new Entities.Period(request.StartDate,request.Length,request.DayLength,request.UserId);
            
            _dbContext.Periods.Add(period);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new PeriodResponse(period.Id, period.StartDate, period.EndDate, period.Length, period.DayLength, period.UserId, period.AmountSpent, period.BudgetAmount);
        }
    }
}

public class CreateFolderEndpoint : ICarterModule
{
    private const string RouteTag = "Periods";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/period", async (CreatePeriodRequest request, ISender sender) =>
        {
            var command = new CreatePeriod.Command
            {
                StartDate = request.StartDate,
                Length = request.Length,
                DayLength = request.DayLength,
                UserId = request.UserId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}