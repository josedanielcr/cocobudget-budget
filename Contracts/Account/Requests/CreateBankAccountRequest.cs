using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Account.Requests;

public class CreateBankAccountRequest
{
    public required string Name { get; set; }
    public required string BankName { get; set; }
    public decimal CurrentBalance { get; set; } = 0;
    public required string Currency { get; set; }
    [MaxLength(4)] public required string AccountNumber { get; set; }
    public required string Notes { get; set; }
    public Guid UserId { get; set; }
}