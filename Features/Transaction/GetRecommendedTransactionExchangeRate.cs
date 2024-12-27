using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Database;
using web_api.Extensions;
using web_api.Shared;

namespace web_api.Features.Transaction;

public static class GetRecommendedTransactionExchangeRate
{
    public class Query : IRequest<Result<decimal>>
    {
        public Guid TransactionId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Query> validator, CurrencyExtension extension)
        : IRequestHandler<Query, Result<decimal>>
    {
        public async Task<Result<decimal>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<decimal>(new Error("GetRecommendedTransactionExchangeRate.TransactionIdNotProvided",
                    $"Transaction with id {request.TransactionId} not found"));
            }
            
            var transaction = await dbContext.Transactions
                .Include(x => x.LinkedAccount)
                .Include(x => x.LinkedCategory)
                    .ThenInclude(x => x.GeneralCategory)
                .Where(x => x.Id == request.TransactionId)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (transaction == null)
            {
                return Result.Failure<decimal>(new Error("GetRecommendedTransactionExchangeRate.TransactionNotFound",
                    $"Transaction with id {request.TransactionId} not found"));
            }
            
            var exchangeRate = await extension.GetExchangeRateAsync(transaction.LinkedAccount.Currency,
                transaction.LinkedCategory.GeneralCategory.Currency);

            return Result<decimal>.Success(exchangeRate.ConversionRate);
        }
    }
}

public class GetRecommendedTransactionExchangeRateEndpoint : ICarterModule
{ 
    private const string RouteTag = "Transactions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/transaction/exchange-rate/{transactionId:guid}", async (Guid transactionId, ISender sender) =>
        {
            
            var query = new GetRecommendedTransactionExchangeRate.Query
            {
                TransactionId = transactionId
            };

            var result = await sender.Send(query);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}