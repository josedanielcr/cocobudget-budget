using Carter;
using FluentValidation;
using MediatR;
using web_api.Contracts.Folder.Requests;
using web_api.Contracts.Folder.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Folder;

public static class CreateFolder
{
    public class Command : IRequest<Result<FolderResponse>>
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Icon).NotEmpty();
            RuleFor(x => x.Color).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<FolderResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<FolderResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = _validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return Result.Failure<FolderResponse>(new Error("CreateFolder.Validation",
                    validationResult.ToString()));
            }

            var folder = new Entities.Folder
            {
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                UserId = request.UserId
            };

            _dbContext.Folders.Add(folder);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new FolderResponse(
                folder.Id,
                folder.Name,
                folder.Icon,
                folder.Color,
                folder.UserId,
                folder.IsActive,
                folder.CreatedOn,
                folder.ModifiedOn
            );
        }
    }
}

public class CreateFolderEndpoint : ICarterModule
{
    private const string RouteTag = "Folders";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/folder", async (CreateFolderRequest request, ISender sender) =>
        {
            var command = new CreateFolder.Command
            {
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                UserId = request.UserId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}