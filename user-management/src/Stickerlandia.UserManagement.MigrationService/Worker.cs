/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Stickerlandia.UserManagement.Agnostic;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

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
        using var activity = s_activitySource.StartActivity("run.migrationWorker", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            var oAuthApplicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var registerUserCommandHandler = scope.ServiceProvider.GetRequiredService<RegisterCommandHandler>();

            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, registerUserCommandHandler, oAuthApplicationManager, roleManager, cancellationToken);
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
        using var runMigrationActivity = s_activitySource.StartActivity("run.migration", ActivityKind.Client);
        
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private static async Task SeedDataAsync(UserManagementDbContext dbContext, RegisterCommandHandler registerHandler,
        IOpenIddictApplicationManager manager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken)
    {
        using var seedData = s_activitySource.StartActivity("run.seedData", ActivityKind.Client);
        
        // The web-ui client is for the public web interface and uses the OAuth2.0 authorization code flow with PKCE.
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
                    new Uri($"{Environment.GetEnvironmentVariable("DEPLOYMENT_HOST_URL") ?? "http://localhost:8080"}/api/app/auth/callback")
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

        // As soon as Stickerlandia services need to call other services under their own identities, add them here

        // Seed default user
        try
        {
            if (!await roleManager.RoleExistsAsync(UserTypes.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(UserTypes.Admin));   
            }
            if (!await roleManager.RoleExistsAsync(UserTypes.User))
            {
                await roleManager.CreateAsync(new IdentityRole(UserTypes.User));   
            }
            
            var user = await registerHandler.Handle(new RegisterUserCommand
            {
                EmailAddress = "user@stickerlandia.com",
                FirstName = "Default",
                LastName = "User",
                Password = "Stickerlandia2025!"
            }, AccountType.User);
            
            seedData?.SetTag("user.default.id", user.AccountId);
        }
        catch (UserExistsException)
        {
            seedData?.SetTag("user.default.exists", true);
        }

        try
        {
            // Seed default admin
            var adminUser = await registerHandler.Handle(new RegisterUserCommand
            {
                EmailAddress = "admin@stickerlandia.com",
                FirstName = "Default",
                LastName = "User",
                Password = "Admin2025!"
            }, AccountType.Admin);
            
            seedData?.SetTag("admin.default.id", adminUser.AccountId);
        }
        catch (UserExistsException)
        {
            seedData?.SetTag("admin.default.exists", true);
        }
    }
}