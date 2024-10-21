using Azure.Identity;

namespace web_api.Configurations;

public static class AddApplicationKeyVault
{
    public static IServiceCollection AddAppKeyVault(this IServiceCollection services, ConfigurationManager configuration)
    {
        configuration.AddAzureKeyVault(
            new Uri(configuration["KeyVault:Uri"]!), 
            new DefaultAzureCredential());
        return services;
    }
}