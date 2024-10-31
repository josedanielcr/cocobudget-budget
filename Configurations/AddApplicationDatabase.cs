using Microsoft.EntityFrameworkCore;
using web_api.Database;

namespace web_api.Configurations;

public static class AddApplicationDatabase
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration["DefaultDbConnectionString"]));

        return services;
    }
}