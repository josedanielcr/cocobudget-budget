using System.ComponentModel.DataAnnotations;
using web_api.Enums;

namespace web_api.Entities;

public class GeneralCategory : BaseEntity
{
    public decimal TargetAmount { get; set; } = 0;
    public CategoryType CategoryType { get; set; }
    public DateTime? FinalDate { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(32)] public string Currency { get; set; } = string.Empty;
}