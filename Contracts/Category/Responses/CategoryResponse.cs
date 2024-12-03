using System.ComponentModel.DataAnnotations;
using web_api.Entities;

namespace web_api.Contracts.Category.Responses;

public class CategoryResponse : BaseEntity
{
    public Guid GeneralId { get; set; }
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    public Guid FolderId { get; set; }
    public required GeneralCategory GeneralCategory { get; set; }
    public Guid GeneralCategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal BudgetAmount { get; set; } 
    public decimal AmountSpent { get; set; }
    public decimal AmountRemaining { get; set; }
}
