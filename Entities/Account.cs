using System.ComponentModel.DataAnnotations;

namespace web_api.Entities;

public class Account : BaseEntity
{
    public required string Name { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    [MaxLength(4)] public required string AccountNumber { get; set; } // just the last 4 digits of the account number for visual properties
    public string Notes { get; set; } = string.Empty;
}