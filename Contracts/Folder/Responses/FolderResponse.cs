using System.ComponentModel.DataAnnotations;
using web_api.Entities;

namespace web_api.Contracts.Folder.Responses;

public class FolderResponse : BaseEntity
{

    //generate the constructor
    public FolderResponse(Guid id, string name, Guid userId, bool isActive,
        DateTime createdOn, DateTime modifiedOn, Guid periodId)
    {
        Id = id;
        Name = name;
        UserId = userId;
        IsActive = isActive;
        CreatedOn = createdOn;
        ModifiedOn = modifiedOn;
        PeriodId = periodId;
    }
    
    [MaxLength(128)] public string Name { get; set; }
    public Guid UserId { get; set; }
    public Guid PeriodId { get; set; }
}