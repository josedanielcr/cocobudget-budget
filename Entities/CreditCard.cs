using System.ComponentModel.DataAnnotations;

namespace web_api.Entities;

public class CreditCard : BaseEntity
{
    public required string Name { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    [MaxLength(4)] public required string AccountNumber { get; set; } // just the last 4 digits of the account number for visual properties
    public string Notes { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal CreditLimit { get; set; }
    [Range(1,31)] public int StatementClosingDay { get; set; } // 1-31 day of the month when the statement of the CC is closed
    public int PaymentOffset { get; set; } // Number of days after the statement closing day when the payment is due
    public List<string> SupportedCurrencies { get; set; } = [];
}