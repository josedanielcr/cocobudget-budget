using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Category.Responses;
using web_api.Contracts.Folder.Responses;
using web_api.Database;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Folder;

public static class GetUserFolders
{
    public class Query : IRequest<Result<List<FolderResponse>>>
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
    
    internal sealed class Handler : IRequestHandler<Query, Result<List<FolderResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Query> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Query> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<List<FolderResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            
            var validationResult = await _validator.ValidateAsync(request,cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<List<FolderResponse>>(new Error("GetUserFolders.Validation",
                    validationResult.ToString()));
            }
            
            var period = await PeriodExtensions.GetUserActivePeriodAsync(_dbContext, request.UserId,cancellationToken);
            
            if (period == null)
            {
                return Result.Failure<List<FolderResponse>>(new Error("CreateFolder.Period",
                    "No active period found for the user"));
            }
            
            var folders = await _dbContext.Folders
                .Include(f => f.Period)
                .Include(f => f.Categories)
                .Where(f => f.UserId == request.UserId && f.Period.Id == period.Id)
                .ToListAsync(cancellationToken);
            
            return folders.Count != 0
                ? folders
                    .Select(f => new FolderResponse(f.Id, f.Name, f.UserId, f.IsActive, f.CreatedOn, f.ModifiedOn,f.Period.Id
                        ,f.Categories!.Select(c => new CategoryResponse
                        {
                            Id = c.Id,
                            GeneralId = c.GeneralId,
                            Name = c.Name,
                            FolderId = c.FolderId,
                            GeneralCategory = c.GeneralCategory,
                            GeneralCategoryId = c.GeneralCategoryId,
                            TargetAmount = c.TargetAmount,
                            BudgetAmount = c.BudgetAmount,
                            AmountSpent = c.AmountSpent,
                            CreatedOn = c.CreatedOn,
                            ModifiedOn = c.ModifiedOn,
                            IsActive = c.IsActive
                        }).ToList()))
                    .ToList() 
                : Result.Failure<List<FolderResponse>>(new Error("Folder.NotFound", $"No folders found for user with id {request.UserId}."));
        }
    }
}

public class GetUserFoldersEndpoint : ICarterModule
{
    private const string RouteTag = "Folders";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/folder/{userId}", async (Guid userId, ISender sender) =>
        {
            var query = new GetUserFolders.Query { UserId = userId };
            var result = await sender.Send(query);
            return result.IsFailure ? Results.NotFound(result.Error) : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}