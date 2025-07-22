using Amazon.Lambda.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Lambda;

public class MigrationFunction(IServiceScopeFactory serviceScopeFactory)
{
    [LambdaFunction]
    public async Task Migrate(object evtData)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await RunMigrationAsync(dbContext, default);
        await SeedDataAsync(dbContext, manager, default);
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
        if (await manager.FindByClientIdAsync("web-ui", cancellationToken) is null)
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-ui",
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
                    // This client requires the Proof Key for Code Exchange (PKCE).
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            }, cancellationToken);
        
        // As soon as Stickerlandia services need to call other services under their own identities, add them here
    }
}