using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Account.Requests;
using web_api.Database;
using web_api.Entities;
using web_api.Shared;

namespace web_api.Features.Accounts;

public static class DeleteBankAccount
{
    public class Command : IRequest<Result>
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

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var bankAccount = await dbContext.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            var validationResult = await ValidateRequestAsync(request, bankAccount, cancellationToken);
            if (!validationResult.IsSuccess)
                return Result.Failure(validationResult.Error);
            
            bankAccount.IsActive = false;
            dbContext.BankAccounts.Update(bankAccount);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        
        private async Task<Result> ValidateRequestAsync(Command request, BankAccount? account, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("DeleteBankAccount.Validation", validationResult.ToString()));
            }
            
            if (account == null)
            {
                return Result.Failure(new Error("DeleteBankAccount.NotFound", $"Bank account with ID {request.Id} was not found."));
            }

            return Result.Success();
        }
    }
}

public class DeleteBankAccountEndpoint : ICarterModule
{
    private const string RouteTag = "Accounts";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/accounts/bank/{id:guid}", async (Guid id, ISender sender) =>
        {
            var command = new DeleteBankAccount.Command
            {
                Id = id,
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}