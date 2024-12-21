using System.ComponentModel.DataAnnotations;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Account.Requests;
using web_api.Contracts.Account.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Shared;

namespace web_api.Features.Accounts;

public static class UpdateBankAccount
{
    public class Command : IRequest<Result<BankAccountResponse>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string BankName { get; set; }
        [MaxLength(4)] public string AccountNumber { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Notes { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.BankName).NotEmpty();
            RuleFor(x => x.AccountNumber).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<BankAccountResponse>>
    {
        public async Task<Result<BankAccountResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var bankAccount = await dbContext.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
            var validationResult = await ValidateRequestAsync(request, bankAccount, cancellationToken);
            if (!validationResult.IsSuccess)
                return Result.Failure<BankAccountResponse>(validationResult.Error);
            
            bankAccount!.Name = request.Name;
            bankAccount.BankName = request.BankName;
            bankAccount.AccountNumber = request.AccountNumber;
            bankAccount.CurrentBalance = request.CurrentBalance;
            bankAccount.Notes = request.Notes;
            dbContext.BankAccounts.Update(bankAccount);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new BankAccountResponse(bankAccount.Id, bankAccount.IsActive,
                bankAccount.CreatedOn, bankAccount.ModifiedOn, bankAccount.Name, bankAccount.BankName,
                bankAccount.CurrentBalance, bankAccount.Currency, bankAccount.AccountNumber, bankAccount.Notes, bankAccount.UserId);
        }
        
        private async Task<Result> ValidateRequestAsync(Command request, BankAccount? account, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("UpdateBankAccount.Validation", validationResult.ToString()));
            }
            
            if (account == null)
            {
                return Result.Failure(new Error("UpdateBankAccount.BankAccount", "Bank account not found"));
            }

            if (request.CurrentBalance < 0)
            {
                return Result.Failure(new Error("CreateBankAccount.CurrentBalance", "Current balance cannot be negative"));
            }

            return Result.Success();
        }
    }
}

public class UpdateBankAccountEndpoint : ICarterModule
{
    private const string RouteTag = "Accounts";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/accounts/bank/{id:guid}", async (Guid id, UpdateBankAccountRequest request, ISender sender) =>
        {
            var command = new UpdateBankAccount.Command
            {
                Id = id,
                Name = request.Name,
                BankName = request.BankName,
                CurrentBalance = request.CurrentBalance,
                AccountNumber = request.AccountNumber,
                Notes = request.Notes,
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}