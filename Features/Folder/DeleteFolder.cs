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

public static class DeleteFolder
{
    public class Command : IRequest<Result<FolderResponse>>
    {
        public Guid Id { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
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
            
            folder.IsActive = false;
            _context.Folders.Update(folder);
            await _context.SaveChangesAsync(cancellationToken);
            
            return new FolderResponse(folder.Id, folder.Name, folder.UserId, folder.IsActive, folder.CreatedOn, folder.ModifiedOn, folder.Period!.Id
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

public class DeleteFolderEndpoint : ICarterModule
{
    private const string RouteTag = "Folders";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/folder/{folderId:guid}", async (Guid folderId, ISender sender) =>
        {
            var query = new DeleteFolder.Command { Id = folderId };
            var result = await sender.Send(query);
            return result.IsFailure ? Results.BadRequest(result.Error) : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}