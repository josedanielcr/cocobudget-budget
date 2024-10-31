using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Category.Requests;

public class CreateCategoryRequest
{
	public string Name { get; set; } = string.Empty; 
	public string Icon { get; set; } = string.Empty;
	public string ColorHex { get; set; } = string.Empty;
	public decimal BudgetAmount { get; set; }
	public Guid FolderId { get; set; }

	public Guid UserId { get; set; }
}
