using System.ComponentModel.DataAnnotations;
using web_api.Entities;

namespace web_api.Contracts.Category.Responses;

public class CategoryResponse : BaseEntity
{
	public CategoryResponse(Guid id, string name, string icon, string colorHex, decimal budgetAmount,
		decimal amountSpent, Guid folderId, bool isActive, DateTime createdOn, DateTime modifiedOn, Guid userId)
	{
		Id = id;
		Name = name;
		Icon = icon;
		ColorHex = colorHex;
		BudgetAmount = budgetAmount;
		AmountSpent = amountSpent;
		FolderId = folderId;
		IsActive = isActive;
		CreatedOn = createdOn;
		ModifiedOn = modifiedOn;
		UserId = userId;
	}

	public string Name { get; set; }
	public string Icon { get; set; }
	public string ColorHex { get; set; }
	public decimal BudgetAmount { get; set; }
	public decimal AmountSpent { get; set; }
	public Guid FolderId { get; set; }

	public Guid UserId { get; set; }
}
