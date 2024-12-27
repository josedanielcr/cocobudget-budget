namespace web_api.Contracts.Transaction.Requests;

public class ReviewCategoryOfTransactionRequest
{
    public Guid TransactionId { get; set; }
    public decimal ExchangeRate { get; set; }
}