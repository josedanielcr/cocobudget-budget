using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace web_api.Entities;

public sealed class Category : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public Guid FolderId { get; set; }
    public Folder Folder { get; set; }

    public decimal BudgetAmount { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountRemaining => BudgetAmount - AmountSpent;
    public Guid UserId { get; set; }
}