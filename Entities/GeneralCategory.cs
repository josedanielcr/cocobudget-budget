using web_api.Enums;

namespace web_api.Entities;

public class GeneralCategory : BaseEntity
{
    public decimal TargetAmount { get; set; } = 0;
    public CategoryType CategoryType { get; set; }
    public DateTime? FinalDate { get; set; }
}