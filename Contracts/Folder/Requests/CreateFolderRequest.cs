using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Folder.Requests;

public class CreateFolderRequest
{
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    [MaxLength(128)] public string Icon { get; set; } = string.Empty;
    [MaxLength(128)] public string Color { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}