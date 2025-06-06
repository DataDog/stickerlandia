using Amazon.Lambda.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Lambda;

public class MigrationFunction(
    ILogger<Sqs> logger,
    IServiceScopeFactory serviceScopeFactory,
    OutboxProcessor outboxProcessor)
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