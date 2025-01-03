using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Folder.Requests;

public class UpdateFolderRequest
{
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}   