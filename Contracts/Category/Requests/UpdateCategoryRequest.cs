using System.ComponentModel.DataAnnotations;
using web_api.Enums;

namespace web_api.Contracts.Category.Requests;

public class UpdateCategoryRequest
{
    public Guid Id { get; set; }
    public CategoryType CategoryType { get; set; }
    public DateTime? FinalDate { get; set; }
    public decimal GeneralTargetAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public required string Name { get; set; }
}