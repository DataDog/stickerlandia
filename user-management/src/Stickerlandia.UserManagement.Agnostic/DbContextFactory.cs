using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresUserRepository(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<UserManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgresUserManagement"), 
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.UserManagement.Agnostic"));
        });

        // Register repositories
        services.AddScoped<IUsers, PostgresUserRepository>();
        services.AddScoped<IOutbox, PostgresUserRepository>();

        return services;
    }
}
