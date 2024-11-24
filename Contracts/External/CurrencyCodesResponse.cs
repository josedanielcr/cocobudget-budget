using System.Text.Json.Serialization;

namespace web_api.Contracts.External;

public class CurrencyCodesResponse
{
    public string result { get; set; } = string.Empty;  
    public string documentation { get; set; } = string.Empty;
    public string terms_of_use { get; set; } = string.Empty;
    
    [JsonPropertyName("supported_codes")]
    public List<List<string>> SupportedCodes { get; set; }
    public List<CurrencyCodes> SupportedCurrencyCodes { get; set; }
    
}

public class CurrencyCodes
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}