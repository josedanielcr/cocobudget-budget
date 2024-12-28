using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Enums;
using web_api.Features.Accounts;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class GetCategoryTransactions
{
    public class Query : IRequest<Result<List<TransactionResponse>>>
    {
        public Guid CategoryId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CategoryId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Query> validator) 
        : IRequestHandler<Query, Result<List<TransactionResponse>>>
    {
        public async Task<Result<List<TransactionResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateRequestAsync(request, cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return Result.Failure<List<TransactionResponse>>(validationResult.Error);
            }

            var transactions = await dbContext.Transactions
                .Where(x => x.LinkedCategoryId == request.CategoryId)
                .Where(x => x.IsActive == true)
                .Where(x => x.Type == TransactionType.Expense)
                .ToListAsync(cancellationToken);
            
            var responseArr = transactions.Select(transaction => new TransactionResponse
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
            }).ToList();

            return responseArr;
        }

        private async Task<Result> ValidateRequestAsync(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("GetCategoryTransactions.CategoryIdNotValid",
                    $"Category id {request.CategoryId} is not valid"));
            }
            return Result.Success();
        }
    }
}

public class GetCategoryTransactionsEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/transaction/category/{categoryId:guid}", async (Guid categoryId, ISender sender) =>
        {
            
            var query = new GetCategoryTransactions.Query
            {
                CategoryId = categoryId
            };

            var result = await sender.Send(query);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}