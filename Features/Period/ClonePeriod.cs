using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Period.Requests;
using web_api.Contracts.Period.Responses;
using web_api.Database;
using web_api.Extensions;
using web_api.Shared;
using web_api.Entities;

namespace web_api.Features.Period;

public static class ClonePeriod
{
    public class Command : IRequest<Result<PeriodResponse>>
    {
        public required Guid UserId { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
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
                return Result.Failure<PeriodResponse>(new Error("ClonePeriod.Validation", validationResult.ToString()));
            }

            var oldPeriod = await PeriodExtensions.GetUserActivePeriodAsync(_dbContext, request.UserId, cancellationToken);
            if (oldPeriod == null)
            {
                return Result.Failure<PeriodResponse>(new Error("ClonePeriod.NotFound", "No se encontró un periodo activo para este usuario."));
            }
            
            Entities.Period newPeriod = new Entities.Period(oldPeriod.EndDate, oldPeriod.Length, oldPeriod.DayLength, request.UserId);
            
            await ReplicateCategoriesAndFolders(_dbContext, oldPeriod, newPeriod, cancellationToken);
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
            return response;
        }
        
        private async Task ReplicateCategoriesAndFolders(ApplicationDbContext dbContext, web_api.Entities.Period oldPeriod, web_api.Entities.Period newPeriod, CancellationToken cancellationToken)
        {
            // Obtener folders del periodo anterior
            var oldFolders = await dbContext.Folders
                .Where(f => f.Period.Id == oldPeriod.Id)
                .Where(f => f.IsActive == true)
                .Include(f => f.Categories)
                .ToListAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
            {
                var newFolder = new Entities.Folder
                {
                    GeneralId = oldFolder.GeneralId,
                    Name = oldFolder.Name,
                    Period = newPeriod,
                    UserId = oldFolder.UserId,
                    IsActive = true
                };
                dbContext.Folders.Add(newFolder);

                // Replicar categorias asociadas a la carpeta
                /*foreach (var oldCategory in oldFolder.Categories ?? Enumerable.Empty<Entities.Category>())
                {
                    var newCategory = new Entities.Category
                    {
                        Name = oldCategory.Name,
                        Folder = newFolder,
                        BudgetAmount = oldCategory.AmountRemaining,
                        AmountSpent = 0, // Restablece el gasto a 0 para el nuevo periodo
                        UserId = oldCategory.UserId,
                        IsActive = true
                    };
                    dbContext.Categories.Add(newCategory);
                }*/
            }
        }
    }
}

public class ClonePeriodEndpoint : ICarterModule
{
    private const string RouteTag = "Periods";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/period/clone/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var command = new ClonePeriod.Command
            {
                UserId = userId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}