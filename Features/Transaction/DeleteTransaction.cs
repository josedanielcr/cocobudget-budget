using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Requests;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Extensions;
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

            var result = await HandleTransactionDelete(transaction);
            transaction.IsActive = false;
            dbContext.Transactions.Update(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }

        private async Task<Result<bool>> HandleTransactionDelete(Entities.Transaction transaction)
        {
            var bankAccount = await dbContext.BankAccounts.FindAsync(transaction.LinkedAccountId);
            if (bankAccount == null)
            {
                return Result.Failure<bool>(new Error("DeleteTransaction.BankAccountNotFound",
                    $"Bank account with id {transaction.LinkedAccountId} not found"));
            }

            return transaction.Type switch
            {
                TransactionType.Income => HandleIncomeTransaction(transaction, bankAccount),
                TransactionType.Expense => await HandleExpenseTransaction(transaction, bankAccount),
                TransactionType.NotTrackable => HandleNotTrackableTransaction(transaction, bankAccount),
                _ => Result.Failure<bool>(new Error("DeleteTransaction.UnknownTransactionType",
                    $"Unknown transaction type {transaction.Type}"))
            };
        }

        private bool HandleNotTrackableTransaction(Entities.Transaction transaction, Entities.BankAccount bankAccount)
        {
            bankAccount.CurrentBalance += transaction.Amount;
            dbContext.BankAccounts.Update(bankAccount);
            return true;
        }

        /*
         * If an expense transaction is deleted, the account balance will be increased by the amount of the transaction.
         * The category balance will be increased by the amount of the transaction. (in case of custom the general category target will be increased as well)
         * if the category has a different currency it needs to be converted to the account 
         */
        private async Task<Result<bool>> HandleExpenseTransaction(Entities.Transaction transaction, Entities.BankAccount bankAccount)
        {
            var category = await dbContext.Categories
                .Include(x => x.GeneralCategory)
                .FirstOrDefaultAsync(x => x.Id == transaction.LinkedCategoryId);
            
            var effect = await dbContext.TransactionCategoryEffects
                .FirstOrDefaultAsync(x => x.TransactionId == transaction.Id && x.CategoryId == transaction.LinkedCategoryId);
            
            if (category == null)
            {
                return Result.Failure<bool>(new Error("DeleteTransaction.CategoryNotFound",
                    $"Category with id {transaction.LinkedCategoryId} not found"));
            }
            
            if (effect == null)
            {
                return Result.Failure<bool>(new Error("DeleteTransaction.TransactionCategoryEffectNotFound",
                    $"TransactionCategoryEffect with transaction id {transaction.Id} and category id {transaction.LinkedCategoryId} not found"));
            }

            TransactionExtensions.HandleExpenseTransactionDelete(dbContext, bankAccount, category, effect);
            return true;
        }

        /*
         * If an income transaction is deleted, the account balance will be decreased by the amount of the transaction.
         */
        private bool HandleIncomeTransaction(Entities.Transaction transaction, Entities.BankAccount bankAccount)
        {
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

public class DeleteTransactionEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/transaction/{transactionId:guid}", async (Guid transactionId, ISender sender) =>
        {
            
            var command = new DeleteTransaction.Command
            {
                TransactionId = transactionId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}