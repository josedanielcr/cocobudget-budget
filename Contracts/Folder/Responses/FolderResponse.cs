using System.ComponentModel.DataAnnotations;
using web_api.Contracts.Category.Responses;
using web_api.Entities;

namespace web_api.Contracts.Folder.Responses;

public class FolderResponse : BaseEntity
{

    //generate the constructor
    public FolderResponse(Guid id, string name, Guid userId, bool isActive,
        DateTime createdOn, DateTime modifiedOn, Guid periodId, List<CategoryResponse> categories)
    {
        Id = id;
        Name = name;
        UserId = userId;
        IsActive = isActive;
        CreatedOn = createdOn;
        ModifiedOn = modifiedOn;
        PeriodId = periodId;
        Categories = categories;
    }
    
    [MaxLength(128)] public string Name { get; set; }
    public Guid UserId { get; set; }
    public Guid PeriodId { get; set; }
    public List<CategoryResponse> Categories { get; set; }
}