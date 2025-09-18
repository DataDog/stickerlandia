/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Amazon.Lambda.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;

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
                ClientSecret = "stickerlandia-web-ui-secret-2025",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                // An implicit consent type is used for the web UI, meaning users will NOT be prompted to consent to requested scoipes.
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                PostLogoutRedirectUris =
                {
                    new Uri("http://localhost:3000")
                },
                RedirectUris =
                {
                    new Uri("http://localhost:8080/api/app/auth/callback")
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