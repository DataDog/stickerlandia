using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;

namespace Stickerlandia.UserManagement.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : IHostedService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, manager, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Stopping migration service", ActivityKind.Client);
        return Task.CompletedTask;
    }

    private static async Task RunMigrationAsync(UserManagementDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync(UserManagementDbContext dbContext, IOpenIddictApplicationManager manager,
        CancellationToken cancellationToken)
    {
        // Add seeding logic here if needed.
        if (await manager.FindByClientIdAsync("user-authentication") is null)
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "user-authentication",
                ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken
                }
            });

        if (await manager.FindByClientIdAsync("internal-service") is null)
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "internal-service",
                ClientSecret = "8E1167EF-5C44-4209-A803-3A109155FDD3",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                }
            });
    }
}