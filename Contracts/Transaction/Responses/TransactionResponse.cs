using web_api.Entities;

namespace web_api.Contracts.Transaction.Responses;

public class TransactionResponse : BaseEntity
{
    public required decimal Amount { get; set; }
    public int Type { get; set; }
    public required Guid LinkedAccountId { get; set; }
    public required Guid LinkedCategoryId { get; set; }
    public string Note { get; set; } = null!;
    public bool RequireCategoryReview { get; set; } = false;
}