using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Category.Responses;
using web_api.Contracts.Folder.Requests;
using web_api.Contracts.Folder.Responses;
using web_api.Database;
using web_api.Features.Category;
using web_api.Shared;

namespace web_api.Features.Folder;

public static class UpdateFolder
{
    public class Command : IRequest<Result<FolderResponse>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<FolderResponse>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDbContext context, IValidator<Command> validator)
        {
            _context = context;
            _validator = validator;
        }
        
        public async Task<Result<FolderResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<FolderResponse>(new Error("Invalid request", validationResult.ToString()));
            }
            
            var folder = await _context.Folders
                .Where(x => x.Id == request.Id)
                .Include(x => x.Period)
                .Include(x => x.Categories)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (folder == null)
            {
                return Result.Failure<FolderResponse>(new Error("Folder not found", $"Folder with id {request.Id} not found"));
            }
            
            folder.Name = request.Name;
            _context.Folders.Update(folder);
            await _context.SaveChangesAsync(cancellationToken);
            return new FolderResponse(folder.Id, folder.Name, folder.UserId, folder.IsActive, folder.CreatedOn, folder.ModifiedOn, folder.Period.Id
                ,folder.Categories!.Select(c => new CategoryResponse
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
                }).ToList());
        }
    }
}

public class UpdateFolderEndpoint : ICarterModule
{
    private const string RouteTag = "Folders";


    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/folder/{folderId:guid}", async (Guid folderId, UpdateFolderRequest request, ISender sender) =>
        {
            var command = new UpdateFolder.Command
            {
                Id = folderId,
                Name = request.Name,
            };
            
            var result = await sender.Send(command);

            return result.IsFailure 
                ? Results.BadRequest(result.Error) 
                : Results.Ok(result);

        }).WithTags(RouteTag);
    }
}