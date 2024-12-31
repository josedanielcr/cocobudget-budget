using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Transaction.Responses;
using web_api.Database;
using web_api.Entities;
using web_api.Enums;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class GetBankAccountInsights
{
    public class Query : IRequest<Result<List<InsightResponse>>>
    {
        public required Guid BankAccountId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BankAccountId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Query> validator,
        IConfiguration configuration) 
        : IRequestHandler<Query, Result<List<InsightResponse>>>
    {
        public async Task<Result<List<InsightResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<List<InsightResponse>>(new Error("GetBankAccountInsights.BankAccountIdNotFound",
                    "BankAccountId is required"));
            }

            var bankAccount = await dbContext.BankAccounts.FindAsync(request.BankAccountId, cancellationToken);
            if (bankAccount == null)
            {
                return Result.Failure<List<InsightResponse>>(new Error("GetBankAccountInsights.BankAccountNotFound",
                    "BankAccount not found"));
            }
            
            var insights = new List<InsightResponse>();
            
            var transactionsToBeReviewed = await GetRequiredReviewTransactionsCount(bankAccount, cancellationToken);
            if (transactionsToBeReviewed > 0)
            {
                insights.Add(new InsightResponse
                {
                    Type = TransactionInsightType.Warning,
                    Message = string.Format(configuration["InsightMessages:RequiredReviewTransaction"]!,transactionsToBeReviewed)
                });
            }
            else
            {
                insights.Add(new InsightResponse
                {
                    Type = TransactionInsightType.Success,
                    Message = string.Format(configuration["InsightMessages:BankAccountClean"]!)
                });
            }
            return insights;
        }

        private async Task<int> GetRequiredReviewTransactionsCount(BankAccount bankAccount, CancellationToken cancellationToken)
        {
            var transactions = await dbContext.Transactions
                .Where(x => x.LinkedAccountId == bankAccount.Id)
                .Where(x => x.IsActive == true)
                .Where(x => x.RequireCategoryReview == true)
                .ToListAsync(cancellationToken);
            
            return transactions.Count();
        }
    }
}

public class GetBankAccountInsightsEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/transaction/insights/{bankAccountId:guid}", async (Guid bankAccountId, ISender sender) =>
        {
            
            var query = new GetBankAccountInsights.Query
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