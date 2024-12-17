using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Account.Responses;

public class BankAccountResponse(
    Guid id,
    bool isActive,
    DateTime createdOn,
    DateTime modifiedOn,
    string name,
    string bankName,
    decimal currentBalance,
    string currency,
    string accountNumber,
    string notes)
{
    public Guid Id { get; set; } = id;
    public bool IsActive { get; set; } = isActive;
    public DateTime CreatedOn { get; set; } = createdOn;
    public DateTime ModifiedOn { get; set; } = modifiedOn;
    public string Name { get; set; } = name;
    public string BankName { get; set; } = bankName;
    public decimal CurrentBalance { get; set; } = currentBalance;
    public string Currency { get; set; } = currency;
    [MaxLength(4)] public string AccountNumber { get; set; } = accountNumber;
    public string Notes { get; set; } = notes;
}