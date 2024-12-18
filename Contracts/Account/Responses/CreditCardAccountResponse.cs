using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Account.Responses;

public class CreditCardAccountResponse(
    Guid id,
    bool isActive,
    DateTime createdOn,
    DateTime modifiedOn,
    string name,
    decimal currentBalance,
    string currency,
    string accountNumber,
    string notes,
    decimal creditLimit,
    int statementClosingDay,
    int paymentOffset,
    List<string> supportedCurrencies,
    Guid userId)
{
    public Guid Id { get; set; } = id;
    public bool IsActive { get; set; } = isActive;
    public DateTime CreatedOn { get; set; } = createdOn;
    public DateTime ModifiedOn { get; set; } = modifiedOn;
    public string Name { get; set; } = name;
    public decimal CurrentBalance { get; set; } = currentBalance;
    public string Currency { get; set; } = currency;
    [MaxLength(4)] public string AccountNumber { get; set; } = accountNumber;
    public string Notes { get; set; } = notes;
    public decimal CreditLimit { get; set; } = creditLimit;
    [Range(1, 31)] public int StatementClosingDay { get; set; } = statementClosingDay;
    public int PaymentOffset { get; set; } = paymentOffset;
    public List<string> SupportedCurrencies { get; set; } = supportedCurrencies;
    public Guid UserId { get; set; } = userId;
}