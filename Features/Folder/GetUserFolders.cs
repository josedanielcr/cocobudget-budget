using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Folder.Responses;
using web_api.Database;
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
            
            var folders = await _dbContext.Folders
                .Where(f => f.UserId == request.UserId)
                .ToListAsync(cancellationToken);
            return folders.Count != 0
                ? folders
                    .Select(f => new FolderResponse(f.Id, f.Name, f.Icon, 
                        f.Color, f.UserId, f.IsActive, f.CreatedOn, f.ModifiedOn))
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
        app.MapGet("api/folders/{userId}", async (Guid userId, ISender sender) =>
        {
            var query = new GetUserFolders.Query { UserId = userId };
            var result = await sender.Send(query);
            return result.IsFailure ? Results.NotFound(result.Error) : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}