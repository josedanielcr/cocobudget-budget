using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Requests;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class GetBankAccountTransactions
{
    public class Query : IRequest<Result<List<TransactionResponse>>>
    {
        public Guid BankAccountId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BankAccountId).NotEmpty();
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
                .Where(x => x.LinkedAccountId == request.BankAccountId)
                .Where(x => x.IsActive == true)
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
                })
                .ToList();

            return responseArr;
        }

        private async Task<Result> ValidateRequestAsync(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("GetBankAccountTransaction.BankAccountNotFound",
                    $"Bank account with id {request.BankAccountId} not found"));
            }
            return Result.Success();
        }
    }
}

public class GetBankAccountTransactionsEndPoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/transaction/bank/{bankAccountId:guid}", async (Guid bankAccountId, ISender sender) =>
        {
            
            var query = new GetBankAccountTransactions.Query
            {
                BankAccountId = bankAccountId
            };

            var result = await sender.Send(query);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}