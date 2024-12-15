namespace web_api.Entities;

public class CreditCard : Account
{
    public decimal CreditLimit { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime StatementClosingDate { get; set; }
    public List<string> SupportedCurrencies { get; set; } = [];
}