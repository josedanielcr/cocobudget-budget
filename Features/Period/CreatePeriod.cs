using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Period.Requests;
using web_api.Contracts.Period.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Enums;
using web_api.Extensions;
using web_api.Shared;
using Microsoft.EntityFrameworkCore;

public static class CreatePeriod
{
    public class Command : IRequest<Result<PeriodResponse>>
    {
        public required DateTime StartDate { get; set; }
        public required PeriodLength Length { get; set; }
        public int DayLength { get; set; }
        public required Guid UserId { get; set; }
        public bool ShouldClone { get; set; }
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

        private bool BeValidDayLength(int dayLength, PeriodLength commandLength)
        {
            // Si el periodo es Custom, entonces los días deben ser mayor que 0
            return commandLength != PeriodLength.Custom || dayLength > 0;
        }

        private bool BeValidStartDate(DateTime startDate, PeriodLength length, int dayLength)
        {
            // La fecha actual debe ser menor o igual que la fecha de inicio más la duración del periodo
            var numberOfDays = PeriodExtensions.GetNumberOfDays(startDate, dayLength, length);
            return DateTime.Now <= startDate.AddDays(numberOfDays);
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<PeriodResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<PeriodResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PeriodResponse>(new Error("CreatePeriod.Validation", validationResult.ToString()));
            }

            web_api.Entities.Period newPeriod = new web_api.Entities.Period(request.StartDate, request.Length, request.DayLength, request.UserId);

            if (request.ShouldClone)
            {
                var oldPeriod = await _dbContext.Periods
                    .Where(p => p.UserId == request.UserId)
                    .OrderByDescending(p => p.EndDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (oldPeriod != null && DateTime.Now > oldPeriod.EndDate)
                {
                    await ReplicateCategoriesAndFolders(oldPeriod, newPeriod, cancellationToken);
                }
            }

            _dbContext.Periods.Add(newPeriod);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new PeriodResponse(
                newPeriod.Id,
                newPeriod.StartDate,
                newPeriod.EndDate,
                newPeriod.Length,
                newPeriod.DayLength,
                newPeriod.UserId,
                newPeriod.AmountSpent,
                newPeriod.BudgetAmount
            );

            return Result.Success(response);
        }

        private async Task ReplicateCategoriesAndFolders(web_api.Entities.Period oldPeriod, web_api.Entities.Period newPeriod, CancellationToken cancellationToken)
        {
            // Obtener folders del periodo anterior
            var oldFolders = await _dbContext.Folders
                .Where(f => f.Period.Id == oldPeriod.Id)
                .Include(f => f.Categories)
                .ToListAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
            {
                var newFolder = new Folder
                {
                    GeneralId = oldFolder.GeneralId,
                    Name = oldFolder.Name,
                    Description = oldFolder.Description,
                    Period = newPeriod,
                    UserId = oldFolder.UserId,
                    IsActive = true
                };
                _dbContext.Folders.Add(newFolder);

                // Replicar categorías asociadas a la carpeta
                //var oldCategories = await _dbContext.Categories
                //    .Where(c => c.FolderId == oldFolder.Id)
                //    .ToListAsync(cancellationToken);

                foreach (var oldCategory in oldFolder.Categories ?? Enumerable.Empty<Category>())
                {
                    var newCategory = new Category
                    {
                        Name = oldCategory.Name,
                        Folder = newFolder,
                        BudgetAmount = oldCategory.AmountRemaining,
                        AmountSpent = 0, // Restablece el gasto a 0 para el nuevo periodo
                        UserId = oldCategory.UserId,
                        IsActive = true
                    };
                    _dbContext.Categories.Add(newCategory);
                    oldCategory.IsActive = false;
                }
                oldFolder.IsActive = false;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public class CreatePeriodEndpoint : ICarterModule
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

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        }).WithTags(RouteTag);
    }
}
