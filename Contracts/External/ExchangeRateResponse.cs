namespace web_api.Contracts.External;

using System.Text.Json.Serialization;

public class ExchangeRateResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("documentation")]
    public string Documentation { get; set; } = string.Empty;

    [JsonPropertyName("terms_of_use")]
    public string TermsOfUse { get; set; } = string.Empty;

    [JsonPropertyName("time_last_update_unix")]
    public long TimeLastUpdateUnix { get; set; } = 0;

    [JsonPropertyName("time_last_update_utc")]
    public string TimeLastUpdateUtc { get; set; } = string.Empty;

    [JsonPropertyName("time_next_update_unix")]
    public long TimeNextUpdateUnix { get; set; } = 0;

    [JsonPropertyName("time_next_update_utc")]
    public string TimeNextUpdateUtc { get; set; } = string.Empty;

    [JsonPropertyName("base_code")]
    public string BaseCode { get; set; } = string.Empty;

    [JsonPropertyName("target_code")]
    public string TargetCode { get; set; } = string.Empty;

    [JsonPropertyName("conversion_rate")]
    public decimal ConversionRate { get; set; } = 0;
}
