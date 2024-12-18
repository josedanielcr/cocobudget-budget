using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using web_api.Contracts.Account.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Accounts;

public static class GetUserBankAccounts
{
    public class Query : IRequest<Result<List<BankAccountResponse>>>
    {
        public Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Query> validator)
        : IRequestHandler<Query, Result<List<BankAccountResponse>>>
    {
        public async Task<Result<List<BankAccountResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateRequestAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
                return Result.Failure<List<BankAccountResponse>>(validationResult.Error);

            var bankAccounts = await dbContext.BankAccounts
                .Where(x => x.UserId == request.UserId)
                .ToListAsync(cancellationToken);

            if (bankAccounts.Count == 0)
            {
                return Result.Failure<List<BankAccountResponse>>(
                    new Error("GetUserBankAccounts.NotFound", "No bank accounts found for the user."));
            }
            
            return bankAccounts.Select(x => new BankAccountResponse(x.Id,
                x.IsActive,x.CreatedOn,x.ModifiedOn,x.Name,x.BankName,x.CurrentBalance,x.Currency,x.AccountNumber,x.Notes,
                x.UserId)).ToList();
        }
        
        private async Task<Result> ValidateRequestAsync(Query request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            return !validationResult.IsValid 
                ? Result.Failure(new Error("CreateBankAccount.Validation", validationResult.ToString())) 
                : Result.Success();
        }
    }
}

public class GetUserBankAccountsEndpoint : ICarterModule
{
    private const string RouteTag = "Accounts";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/accounts/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var command = new GetUserBankAccounts.Query
            {
                UserId = userId
            };
            var result = await sender.Send(command);
            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
