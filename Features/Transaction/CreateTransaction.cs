using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Requests;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Enums;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class CreateTransaction
{
    public class Command : IRequest<Result<TransactionResponse>>
    {
        public required decimal Amount { get; set; }
        public int Type { get; set; }
        public required Guid LinkedAccountId { get; set; }
        public required Guid LinkedCategoryId { get; set; }
        public string Note { get; set; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Type).GreaterThanOrEqualTo(0).LessThanOrEqualTo(2);
            RuleFor(x => x.LinkedAccountId).NotEmpty();
            RuleFor(x => x.LinkedCategoryId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator) 
        : IRequestHandler<Command, Result<TransactionResponse>>
    {
        public async Task<Result<TransactionResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = await dbContext.Categories
                .Include(x => x.GeneralCategory)
                .FirstOrDefaultAsync(x => x.Id == request.LinkedCategoryId, cancellationToken);
            var account = await dbContext.BankAccounts
                .FindAsync(request.LinkedAccountId, cancellationToken);  // for now till credit card is implemented
            
            var validationResult = await ValidateRequestAsync(request, cancellationToken, category, account);

            if (!validationResult.IsSuccess)
            {
                return Result.Failure<TransactionResponse>(validationResult.Error);
            }
            
            var transaction = new Entities.Transaction
            {
                Amount = request.Amount,
                Type = (TransactionType) request.Type,
                LinkedAccountId = request.LinkedAccountId,
                LinkedCategoryId = request.LinkedCategoryId,
                Note = request.Note
            };
            dbContext.Transactions.Add(transaction);
            HandlePostTransaction(transaction, account!, category!);
            
            await dbContext.SaveChangesAsync(cancellationToken);

            return new TransactionResponse
            {
                Id = transaction.Id,
                CreatedOn = transaction.CreatedOn,
                ModifiedOn = transaction.ModifiedOn,
                IsActive = transaction.IsActive,
                Amount = transaction.Amount,
                Type = (int)transaction.Type,
                LinkedAccountId = transaction.LinkedAccountId,
                LinkedCategoryId = transaction.LinkedCategoryId,
                Note = transaction.Note,
                RequireCategoryReview = transaction.RequireCategoryReview
            };
        }

        // If the transaction currency is different from the account currency it needs to be reviewed so the app knows
        // with which exchange rate to convert the amount
        private void HandlePostTransaction(Entities.Transaction transaction, BankAccount account, Entities.Category category)
        {
            account.CurrentBalance -= transaction.Amount;
            if (category.GeneralCategory.Currency != account.Currency)
            {
                transaction.RequireCategoryReview = true;
                return;
            }
            
            category.AmountSpent += transaction.Amount;
            category.AmountRemaining = category.TargetAmount - category.AmountSpent;
            
            // Custom categories have a target that should decrease when a transaction is made / fixed categories don't
            if (category.GeneralCategory.CategoryType == CategoryType.Custom)
            {
                category.GeneralCategory.TargetAmount -= transaction.Amount;
            }
        }

        private async Task<Result> ValidateRequestAsync(Command request, CancellationToken cancellationToken,
            Entities.Category? category, BankAccount? account)
        {
            if (category is null)
            {
                return Result.Failure(new Error("CreateTransaction.CategoryNotFound",
                    $"Category with id {request.LinkedCategoryId} not found"));
            }

            if (account is null)
            {
                return Result.Failure(new Error("CreateTransaction.AccountNotFound",
                    $"Account with id {request.LinkedAccountId} not found"));
            }
            
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("CreateTransaction.Validation", validationResult.ToString()));
            }

            if (request.Amount <= 0)
            {
                return Result.Failure(new Error("CreateTransaction.Amount",
                    "Amount must be greater than 0"));
            }
            
            if(account.IsActive == false)
            {
                return Result.Failure(new Error("CreateTransaction.InactiveAccount",
                    "Account is inactive"));
            }
            
            if(account.CurrentBalance < request.Amount)
            {
                return Result.Failure(new Error("CreateTransaction.InsufficientFunds",
                    "Insufficient funds in account"));
            }
            
            if(category.GeneralCategory.TargetAmount < request.Amount)
            {
                return Result.Failure(new Error("CreateTransaction.TargetAmountExceeded",
                    "Amount exceeds target amount"));
            }
            
            return Result.Success();
        }
    }
}

public class CreateTransactionEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/transaction", async (CreateTransactionRequest request, ISender sender) =>
        {
            
            var command = new CreateTransaction.Command
            {
                Amount = request.Amount,
                Type = request.Type,
                LinkedAccountId = request.LinkedAccountId,
                LinkedCategoryId = request.LinkedCategoryId,
                Note = request.Note
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}