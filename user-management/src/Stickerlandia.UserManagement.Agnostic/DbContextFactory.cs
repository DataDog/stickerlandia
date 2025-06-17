using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresUserRepository(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("database"), 
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.UserManagement.Agnostic"));
        });

        // Register repositories
        

        return services;
    }
}
