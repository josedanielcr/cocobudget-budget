using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class DeleteTransaction
{
    public class Command : IRequest<Result<bool>>
    {
        public Guid TransactionId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator) : IRequestHandler<Command, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateRequestAsync(request, cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return Result.Failure<bool>(validationResult.Error);
            }
            
            var transaction = await dbContext.Transactions.FindAsync(request.TransactionId);
            if (transaction == null)
            {
                return Result.Failure<bool>(new Error("DeleteTransaction.TransactionIdNotFound",
                    $"Transaction with id {request.TransactionId} not found"));
            }

            return await HandleTransactionDelete(transaction);
        }

        private async Task<Result<bool>> HandleTransactionDelete(Entities.Transaction transaction)
        {
            switch (transaction.Type)
            {
                case TransactionType.Income:
                    return await HandleIncomeTransaction(transaction);
                case TransactionType.Expense:
                    return await HandleExpenseTransaction(transaction);
                case TransactionType.NotTrackable:
                    return await HandleNotTrackableTransaction(transaction);
                default:
                    return Result.Failure<bool>(new Error("DeleteTransaction.UnknownTransactionType",
                        $"Unknown transaction type {transaction.Type}"));
            }
        }

        private async Task<Result<bool>> HandleNotTrackableTransaction(Entities.Transaction transaction)
        {
            throw new NotImplementedException();
        }

        /*
         * If an expense transaction is deleted, the account balance will be increased by the amount of the transaction.
         * The category balance will be increased by the amount of the transaction. (in case of custom the general category target will be increased as well)
         * if the category has a different currency it needs to be converted to the account 
         */
        private async Task<Result<bool>> HandleExpenseTransaction(Entities.Transaction transaction)
        {
            throw new NotImplementedException();
        }

        /*
         * If an income transaction is deleted, the account balance will be decreased by the amount of the transaction.
         */
        private async Task<Result<bool>> HandleIncomeTransaction(Entities.Transaction transaction)
        {
            var bankAccount = await dbContext.BankAccounts.FindAsync(transaction.LinkedAccountId);
            if (bankAccount == null)
            {
                return Result.Failure<bool>(new Error("DeleteTransaction.BankAccountNotFound",
                    $"Bank account with id {transaction.LinkedAccountId} not found"));
            }
            bankAccount.CurrentBalance -= transaction.Amount;
            dbContext.BankAccounts.Update(bankAccount);
            return true;
        }

        private async Task<Result> ValidateRequestAsync(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("DeleteTransaction.TransactionIdNotFound",
                    $"Transaction with id {request.TransactionId} not found"));
            }
            return Result.Success();
        }
    }
}