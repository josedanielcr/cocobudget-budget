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

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<PeriodResponse>>
    {
        public async Task<Result<PeriodResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<PeriodResponse>(new Error("ClonePeriod.Validation", validationResult.ToString()));
            }

            var currentPeriod = await dbContext.Periods
                .Where(x => x.UserId == request.UserId)
                .Where(x => x.IsActive == true)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (currentPeriod == null)
            {
                return Result.Failure<PeriodResponse>(new Error("ClonePeriod.NotFound", "No active period found"));
            }

            try
            {
                var newPeriod = CloneCurrentPeriod(currentPeriod);
                await CloneCurrPeriodFoldersToNewPeriod(currentPeriod, newPeriod, cancellationToken);
                currentPeriod.IsActive = false;
                dbContext.Periods.Update(currentPeriod);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new PeriodResponse(
                    newPeriod.Id,
                    newPeriod.StartDate,
                    newPeriod.EndDate,
                    newPeriod.Length,
                    newPeriod.DayLength,
                    newPeriod.UserId,
                    newPeriod.AmountSpent,
                    newPeriod.BudgetAmount
                );
            }
            catch (Exception e)
            {
                return Result.Failure<PeriodResponse>(new Error("ClonePeriod.Error", e.Message));
            }
        }

        private async Task CloneCurrPeriodFoldersToNewPeriod(Entities.Period currentPeriod, Entities.Period newPeriod, CancellationToken cancellationToken)
        {
            var activeCurrPeriodFolders = await dbContext.Folders
                .Where(x => x.Period.Id == currentPeriod.Id)
                .Where(x => x.IsActive == true)
                .ToListAsync(cancellationToken);
            
            foreach (var folder in activeCurrPeriodFolders)
            {
                var newFolder = new Entities.Folder
                {
                    Name = folder.Name,
                    GeneralId = folder.GeneralId,
                    Period = newPeriod,
                    UserId = newPeriod.UserId,
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now,
                    IsActive = true
                };
                newFolder.Categories = await CloneCurrFolderCategoriesToNewFolder(folder, newFolder, cancellationToken);
                dbContext.Folders.Add(newFolder);
            }
        }

        private async Task<List<Entities.Category>?> CloneCurrFolderCategoriesToNewFolder(Entities.Folder currFolder, Entities.Folder newFolder, CancellationToken cancellationToken)
        {
            var activeFolderCategories = await dbContext.Categories
                .Include(x => x.GeneralCategory)
                .Where(x => x.FolderId == currFolder.Id)
                .Where(x => x.IsActive == true)
                .ToListAsync(cancellationToken);
            
            var newFolderCategories = new List<Entities.Category>();
            foreach (var category in activeFolderCategories)
            {
                var generalCategoryUpdated = await UpdateGeneralCategory(category);
                var newCategory = new Entities.Category
                {
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now,
                    IsActive = true,
                    GeneralId = category.GeneralId,
                    Name = category.Name,
                    Folder = newFolder,
                    FolderId = newFolder.Id,
                    GeneralCategory = generalCategoryUpdated,
                    GeneralCategoryId = generalCategoryUpdated.Id,
                    TargetAmount = category.TargetAmount,
                    BudgetAmount = 0,
                    AmountSpent = 0,
                    AmountRemaining = category.TargetAmount - category.AmountSpent
                };
                dbContext.Categories.Add(newCategory);
                newFolderCategories.Add(newCategory);
            }
            return newFolderCategories;
        }

        private async Task<GeneralCategory> UpdateGeneralCategory(Entities.Category category)
        {
            var generalCategory = await dbContext.GeneralCategories
                .Where(x => x.Id == category.GeneralCategoryId)
                .FirstOrDefaultAsync();

            if (generalCategory == null)
            {
                throw new BadHttpRequestException("General category not found");
            }
            
            generalCategory.TargetAmount -= category.AmountSpent;
            dbContext.GeneralCategories.Update(generalCategory);
            return generalCategory;
        }

        private Entities.Period CloneCurrentPeriod(Entities.Period currentPeriod)
        {
            var period = new Entities.Period(currentPeriod.EndDate.AddDays(1), currentPeriod.Length,
                currentPeriod.DayLength, currentPeriod.UserId);
            dbContext.Periods.Add(period);
            return period;
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