using System.ComponentModel.DataAnnotations;

namespace web_api.Entities;

public sealed class Folder : BaseEntity
{
    public Guid GeneralId { get; set; } = Guid.NewGuid(); // GeneralId is used to identify the folder even if it's across different periods
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    [MaxLength(128)] public string Icon { get; set; } = string.Empty;
    [MaxLength(128)] public string Color { get; set; } = string.Empty;

    public Period Period { get; set; }
    public Guid UserId { get; set; }

    // Opcional: Relaci�n con Category
    public List<Category>? Categories { get; set; }
}