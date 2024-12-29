using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Requests;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class ReviewCategoryOfTransaction
{
    public class Command : IRequest<Result<TransactionResponse>>
    {
        public Guid TransactionId { get; set; }
        public decimal ExchangeRate { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionId).NotEmpty();
            RuleFor(x => x.ExchangeRate).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<TransactionResponse>>
    {
        public async Task<Result<TransactionResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
                        
            var transaction = await dbContext.Transactions
                .Include(x => x.LinkedAccount)
                .Where(x => x.Id == request.TransactionId)
                .FirstOrDefaultAsync(cancellationToken);
            
            var category = await dbContext.Categories
                .Include(x => x.GeneralCategory)
                .Where(x => x.Id == transaction!.LinkedCategoryId)
                .FirstOrDefaultAsync(cancellationToken);
            
            var validationResult = await ValidateRequestAsync(request, cancellationToken, transaction, category);

            if (!validationResult.IsSuccess)
            {
                return Result.Failure<TransactionResponse>(validationResult.Error);
            }
            
            var effect = await dbContext.TransactionCategoryEffects
                .Where(x => x.TransactionId == transaction!.Id)
                .Where(x => x.CategoryId == category!.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (effect == null)
            {
                return Result.Failure<TransactionResponse>(new Error("ReviewCategoryOfTransaction.EffectNotFound",
                    $"Effect for transaction with id {request.TransactionId} and category with id {category.Id} not found"));
            }
            
            ExecuteTransactionConversion(request, transaction!, category!, effect);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new TransactionResponse
            {
                Id = transaction!.Id,
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

        private void ExecuteTransactionConversion(Command request, Entities.Transaction transaction,
            Entities.Category category, Entities.TransactionCategoryEffect effect)
        {
            var categoryAmount = transaction!.Amount * request.ExchangeRate;
            category!.AmountSpent += categoryAmount;
            category.AmountRemaining = category.TargetAmount - category.AmountSpent;
            
            // Custom categories have a target that should decrease when a transaction is made / fixed categories don't
            if (category.GeneralCategory.CategoryType == CategoryType.Custom)
            {
                category.GeneralCategory.TargetAmount -= categoryAmount;
            }

            transaction.RequireCategoryReview = false;
            
            //update the transaction effect
            effect.Amount = categoryAmount;
            effect.ConversionRate = request.ExchangeRate;
            
            dbContext.Update(effect);
            dbContext.Update(transaction);
            dbContext.Update(category);
        }

        private async Task<Result> ValidateRequestAsync(Command request, CancellationToken cancellationToken,
            Entities.Transaction? transaction, Entities.Category? category)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("ReviewCategoryOfTransaction.TransactionIdNotProvided",
                    $"Transaction with id {request.TransactionId} not found"));
            }
            
            if (transaction == null)
            {
                return Result.Failure(new Error("ReviewCategoryOfTransaction.TransactionNotFound",
                    $"Transaction with id {request.TransactionId} not found"));
            }
            
            if (category == null)
            {
                return Result.Failure(new Error("ReviewCategoryOfTransaction.CategoryNotFound",
                    $"Category with id {transaction.LinkedCategoryId} not found"));
            }

            if (!transaction.RequireCategoryReview)
            {
                return Result.Failure(new Error("ReviewCategoryOfTransaction.TransactionAlreadyReviewed",
                    $"Transaction with id {request.TransactionId} already reviewed"));
            }

            if (category.GeneralCategory.Currency == transaction.LinkedAccount.Currency)
            {
                return Result.Failure(new Error("ReviewCategoryOfTransaction.CurrencyMismatch",
                    $"Transaction currency {transaction.LinkedAccount.Currency} and category" +
                    $"currency {category.GeneralCategory.Currency} are the same"));
            }
            
            return Result.Success();
        }
    }
}

public class ReviewCategoryOfTransactionEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/transaction/review", async (ReviewCategoryOfTransactionRequest request, ISender sender) =>
        {
            
            var query = new ReviewCategoryOfTransaction.Command
            {
                TransactionId = request.TransactionId,
                ExchangeRate = request.ExchangeRate
            };

            var result = await sender.Send(query);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}