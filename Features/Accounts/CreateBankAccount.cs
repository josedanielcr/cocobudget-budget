using System.ComponentModel.DataAnnotations;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Account.Requests;
using web_api.Contracts.Account.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Features.Category;
using web_api.Shared;

namespace web_api.Features.Accounts;

public static class CreateBankAccount
{
    public class Command : IRequest<Result<BankAccountResponse>>
    {
        public required Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string BankName { get; set; }
        public decimal CurrentBalance { get; set; } = 0;
        public required string Currency { get; set; }
        [MaxLength(4)] public required string AccountNumber { get; set; }
        public required string Notes { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.BankName).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty();
            RuleFor(x => x.AccountNumber).NotEmpty();
            RuleFor(x => x.Notes).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<BankAccountResponse>>
    {
        public async Task<Result<BankAccountResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            
            var validationResult = await ValidateRequestAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
                return Result.Failure<BankAccountResponse>(validationResult.Error);
            
            var newBankAccount = new BankAccount
            {
                Name = request.Name,
                BankName = request.BankName,
                CurrentBalance = request.CurrentBalance,
                Currency = request.Currency,
                AccountNumber = request.AccountNumber,
                Notes = request.Notes,
                UserId = request.UserId
            };
            
            await dbContext.BankAccounts.AddAsync(newBankAccount, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return new BankAccountResponse(
                newBankAccount.Id,
                newBankAccount.IsActive,
                newBankAccount.CreatedOn,
                newBankAccount.ModifiedOn,
                newBankAccount.Name,
                newBankAccount.BankName,
                newBankAccount.CurrentBalance,
                newBankAccount.Currency,
                newBankAccount.AccountNumber,
                newBankAccount.Notes,
                newBankAccount.UserId
            );
        }
        
        private async Task<Result> ValidateRequestAsync(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("CreateBankAccount.Validation", validationResult.ToString()));
            }

            var bankAccountExists = await dbContext.BankAccounts
                .AnyAsync(x => x.AccountNumber == request.AccountNumber, cancellationToken);
            if (bankAccountExists)
            {
                return Result.Failure(new Error("CreateBankAccount.AccountNumber", "Account number already exists"));
            }

            if (request.CurrentBalance < 0)
            {
                return Result.Failure(new Error("CreateBankAccount.CurrentBalance", "Current balance cannot be negative"));
            }
            
            if(request.UserId == Guid.Empty)
            {
                return Result.Failure(new Error("CreateBankAccount.UserId", "User ID cannot be empty"));
            }

            return Result.Success();
        }
    }
}

public class CreateBankAccountEndpoint : ICarterModule
{
    private const string RouteTag = "Accounts";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/accounts/bank", async (CreateBankAccountRequest request, ISender sender) =>
        {
            var command = new CreateBankAccount.Command
            {
                Name = request.Name,
                BankName = request.BankName,
                CurrentBalance = request.CurrentBalance,
                Currency = request.Currency,
                AccountNumber = request.AccountNumber,
                Notes = request.Notes,
                UserId = request.UserId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
