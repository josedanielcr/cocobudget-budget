using System.ComponentModel.DataAnnotations;

namespace web_api.Entities;

public class CreditCard : Account
{
    public decimal CreditLimit { get; set; }
    [Range(1,31)] public int StatementClosingDay { get; set; } // 1-31 day of the month when the statement of the CC is closed
    public int PaymentOffset { get; set; } // Number of days after the statement closing day when the payment is due
    public List<string> SupportedCurrencies { get; set; } = [];
}