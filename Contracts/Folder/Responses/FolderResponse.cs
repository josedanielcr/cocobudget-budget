using System.ComponentModel.DataAnnotations;
using web_api.Entities;

namespace web_api.Contracts.Folder.Responses;

public class FolderResponse : BaseEntity
{

    //generate the constructor
    public FolderResponse(Guid id, string name, string icon, string color, Guid userId, bool isActive,
        DateTime createdOn, DateTime modifiedOn)
    {
        Id = id;
        Name = name;
        Icon = icon;
        Color = color;
        UserId = userId;
        IsActive = isActive;
        CreatedOn = createdOn;
        ModifiedOn = modifiedOn;
    }
    
    [MaxLength(128)] public string Name { get; set; }
    [MaxLength(128)] public string Icon { get; set; }
    [MaxLength(128)] public string Color { get; set; }
    public Guid UserId { get; set; }
}