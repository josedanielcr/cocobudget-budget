using web_api.Enums;

namespace web_api.Entities;

public class Transaction : BaseEntity
{
    public required decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public BankAccount LinkedAccount { get; set; } = null!;
    public required Guid LinkedAccountId { get; set; }
    public Category LinkedCategory { get; set; } = null!;
    public Guid? LinkedCategoryId { get; set; }
    public string Note { get; set; } = null!;
    public bool RequireCategoryReview { get; set; } = false;
}