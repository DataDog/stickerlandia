using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;

// Allow catch of a generic exception in the worker to ensure the worker failing doesn't crash the entire application.
#pragma warning disable CA1031
// This is a worker service that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

namespace Stickerlandia.UserManagement.MigrationService;

internal sealed class Worker(
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
        catch (Exception)
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
        if (await manager.FindByClientIdAsync("user-authentication", cancellationToken) is null)
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "user-authentication",
                ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:3000")
                },
                RedirectUris =
                {
                    new Uri("https://localhost:3000/callback")
                },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        //
        // if (await manager.FindByClientIdAsync("internal-service", cancellationToken) is null)
        //     await manager.CreateAsync(new OpenIddictApplicationDescriptor
        //     {
        //         ClientId = "internal-service",
        //         ClientSecret = "8E1167EF-5C44-4209-A803-3A109155FDD3",
        //         Permissions =
        //         {
        //             OpenIddictConstants.Permissions.Endpoints.Token,
        //             OpenIddictConstants.Permissions.Endpoints.Authorization,
        //             OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
        //         },
        //         RedirectUris = { new Uri("http://localhost:3000") }
        //     }, cancellationToken);
    }
}