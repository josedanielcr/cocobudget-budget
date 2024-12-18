using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Account.Requests;

public class CreateCreditCardAccountRequest
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