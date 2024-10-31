using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace web_api.Entities;

public sealed class Folder : BaseEntity
{
    [MaxLength(128)] public string Name { get; set; } = string.Empty;
    [MaxLength(128)] public string Icon { get; set; } = string.Empty;
    [MaxLength(128)] public string Color { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    // Opcional: Relación con Category
    public List<Category>? Categories { get; set; }
}