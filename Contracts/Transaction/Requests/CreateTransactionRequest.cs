namespace web_api.Contracts.Transaction.Requests;

public class CreateTransactionRequest
{
    public required decimal Amount { get; set; }
    public int Type { get; set; }
    public required Guid LinkedAccountId { get; set; }
    public required Guid LinkedCategoryId { get; set; }
    public string Note { get; set; } = null!;
}