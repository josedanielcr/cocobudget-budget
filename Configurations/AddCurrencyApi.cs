namespace web_api.Configurations;

public static class AddCurrencyApi
{
    public static IServiceCollection AddAppCurrencyApi(this IServiceCollection services, ConfigurationManager configuration)
    {
        var exchangeRateApiUrl = configuration["ExchangeRateApi:Uri"];
        var exchangeRateApiName = configuration["ExchangeRateApi:Name"];
        
        services.AddHttpClient(exchangeRateApiName!, client =>
        {
            client.BaseAddress = new Uri(exchangeRateApiUrl!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}