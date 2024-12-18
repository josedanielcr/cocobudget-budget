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

public static class CreateCreditCard
{
    public class Command : IRequest<Result<CreditCardAccountResponse>>
    {
        public required string Name { get; set; }
        public required string Currency { get; set; }
        public decimal CurrentBalance { get; set; } = 0;
        [MaxLength(4)] public required string AccountNumber { get; set; }
        public decimal CreditLimit { get; set; }
        [Range(1,31)] public int StatementClosingDay { get; set; } 
        public int PaymentOffset { get; set; }
        public List<string> SupportedCurrencies { get; set; } = [];
        public required string Notes { get; set; }
        public Guid UserId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty();
            RuleFor(x => x.AccountNumber).NotEmpty();
            RuleFor(x => x.CreditLimit).GreaterThan(0);
            RuleFor(x => x.StatementClosingDay).NotEmpty();
            RuleFor(x => x.PaymentOffset).NotEmpty();
            RuleFor(x => x.SupportedCurrencies).NotEmpty();
            RuleFor(x => x.Notes).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<CreditCardAccountResponse>>
    {
        public async Task<Result<CreditCardAccountResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateRequestAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
                return Result.Failure<CreditCardAccountResponse>(validationResult.Error);

            var newCreditCard = new CreditCard
            {
                Name = request.Name,
                Currency = request.Currency,
                CurrentBalance = request.CurrentBalance,
                AccountNumber = request.AccountNumber,
                CreditLimit = request.CreditLimit,
                StatementClosingDay = request.StatementClosingDay,
                PaymentOffset = request.PaymentOffset,
                SupportedCurrencies = request.SupportedCurrencies,
                Notes = request.Notes,
                UserId = request.UserId
            };
            
            dbContext.CreditCards.Add(newCreditCard);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new CreditCardAccountResponse(
                newCreditCard.Id,
                newCreditCard.IsActive,
                newCreditCard.CreatedOn,
                newCreditCard.ModifiedOn,
                newCreditCard.Name,
                newCreditCard.CurrentBalance,
                newCreditCard.Currency,
                newCreditCard.AccountNumber,
                newCreditCard.Notes,
                newCreditCard.CreditLimit,
                newCreditCard.StatementClosingDay,
                newCreditCard.PaymentOffset,
                newCreditCard.SupportedCurrencies,
                newCreditCard.UserId);
        }

        private async Task<Result> ValidateRequestAsync(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure(new Error("CreateBankAccount.Validation", validationResult.ToString()));
            }

            var creditCard = await dbContext.CreditCards.FirstOrDefaultAsync(x => x.AccountNumber
                == request.AccountNumber, cancellationToken);
            if (creditCard != null)
            {
                return Result.Failure(new Error("CreateBankAccount.Validation", "Account number already exists"));
            }
            
            if(request.UserId == Guid.Empty)
            {
                return Result.Failure(new Error("CreateBankAccount.UserId", "User ID cannot be empty"));
            }
            
            return Result.Success();
        }
    }
}

public class CreateCreditCardAccountEndpoint : ICarterModule
{
    private const string RouteTag = "Accounts";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/accounts/credit-card", async (CreateCreditCardAccountRequest request, ISender sender) =>
        {
            var command = new CreateCreditCard.Command
            {
                Name = request.Name,
                CurrentBalance = request.CurrentBalance,
                Currency = request.Currency,
                AccountNumber = request.AccountNumber,
                CreditLimit = request.CreditLimit,
                StatementClosingDay = request.StatementClosingDay,
                PaymentOffset = request.PaymentOffset,
                SupportedCurrencies = request.SupportedCurrencies,
                Notes = request.Notes,
                UserId = request.UserId
            };

            var result = await sender.Send(command);

            return result.IsFailure
                ? Results.BadRequest(result)
                : Results.Ok(result);
        }).WithTags(RouteTag);
    }
}
