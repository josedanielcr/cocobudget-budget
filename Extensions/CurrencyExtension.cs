using System.Text.Json;
using web_api.Contracts.External;

namespace web_api.Extensions;

public class CurrencyExtension(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    private readonly HttpClient _httpClientFactory = httpClientFactory.CreateClient(configuration["ExchangeRateApi:Name"]!);
    private readonly string _exchangeRateApiKey = configuration["ExchangeRateApiKey"]!;

    public async Task<CurrencyCodesResponse> GetCurrencyCodesAsync()
    {
        var endpoint = configuration["ExchangeRateApi:Endpoints:CurrencyCodes"];
        var response = await _httpClientFactory.GetAsync(_exchangeRateApiKey +"/"+ endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var currencyCodesResponse = JsonSerializer.Deserialize<CurrencyCodesResponse>(content);
        var supportedCurrencies = this.GetSupportedCurrenciesFromResult(content!);
        currencyCodesResponse!.SupportedCurrencyCodes = supportedCurrencies;
        currencyCodesResponse.SupportedCodes = [];
        return  currencyCodesResponse 
                ?? throw new Exception("Failed to deserialize currency codes response");
    }

    private List<CurrencyCodes> GetSupportedCurrenciesFromResult(string content)
    {
        var response = JsonSerializer.Deserialize<CurrencyCodesResponse>(content);

        if (response?.SupportedCodes == null)
        {
            throw new Exception("Invalid API response: supported_codes is null.");
        }
        
        var currencies = response.SupportedCodes.Select(codeArray => new CurrencyCodes
        {
            Code = codeArray[0],
            Name = codeArray[1]
        }).ToList();

        return currencies;
    }

    public async Task<ExchangeRateResponse> GetExchangeRateAsync(string baseCurrency, string targetCurrency)
    {
        var uri = configuration["ExchangeRateApi:Uri"];
        var endpoint = uri + _exchangeRateApiKey + "/pair/" + baseCurrency + "/" + targetCurrency;
        var response = await _httpClientFactory.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var exchangeRateResponse = JsonSerializer.Deserialize<ExchangeRateResponse>(content);
        return exchangeRateResponse 
               ?? throw new Exception("Failed to deserialize exchange rate response");
    }
}