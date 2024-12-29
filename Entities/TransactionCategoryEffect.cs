namespace web_api.Entities;

public class TransactionCategoryEffect : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
    public decimal Amount { get; set; }
    public decimal? ConversionRate { get; set; }
}