using web_api.Enums;

namespace web_api.Contracts.Category.Requests;

public class CreateCategoryRequest
{
    public required Guid UserId { get; set; }
    public required Guid FolderId { get; set; }
    public CategoryType CategoryType { get; set; } = CategoryType.Fixed;
    public DateTime? FinalDate { get; set; }
    public required string Currency { get; set; }
    public required decimal GeneralTargetAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public required string Name { get; set; }
}
