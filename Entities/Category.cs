using System.ComponentModel.DataAnnotations;

namespace web_api.Entities;

public sealed class Category : BaseEntity
{
    public Guid GeneralId { get; set; }
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    public Guid FolderId { get; set; }
    public Folder Folder { get; set; } = null!;
    public required GeneralCategory GeneralCategory { get; set; }
    public Guid GeneralCategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountRemaining => BudgetAmount - AmountSpent;
}