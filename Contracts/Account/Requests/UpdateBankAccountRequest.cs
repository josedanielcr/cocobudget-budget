using System.ComponentModel.DataAnnotations;

namespace web_api.Contracts.Account.Requests;

public class UpdateBankAccountRequest
{
    public string Name { get; set; }
    public string BankName { get; set; }
    [MaxLength(4)] public string AccountNumber { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Notes { get; set; }
}