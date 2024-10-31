using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Category.Requests;

public class UpdateCategoryRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
}