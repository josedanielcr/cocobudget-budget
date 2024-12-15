using System.ComponentModel.DataAnnotations;
using FluentValidation;
using MediatR;
using web_api.Contracts.Account.Responses;
using web_api.Database;
using web_api.Shared;

namespace web_api.Features.Accounts;

public static class CreateBankAccount
{
    public class Command : IRequest<Result<BankAccountResponse>>
    {
        public required string Name { get; set; }
        public required string BankName { get; set; }
        public decimal CurrentBalance { get; set; } = 0;
        public required string Currency { get; set; }
        [MaxLength(4)] public required string AccountNumber { get; set; }
        public required string Notes { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.BankName).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty();
            RuleFor(x => x.AccountNumber).NotEmpty();
            RuleFor(x => x.Notes).NotEmpty();
        }
    }

    internal sealed class Handler(ApplicationDbContext dbContext, IValidator<Command> validator)
        : IRequestHandler<Command, Result<BankAccountResponse>>
    {
        public Task<Result<BankAccountResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}