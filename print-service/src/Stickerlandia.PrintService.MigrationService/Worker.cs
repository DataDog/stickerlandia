/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Stickerlandia.PrintService.Agnostic.Data;

// Allow catch of a generic exception in the worker to ensure the worker failing doesn't crash the entire application.
#pragma warning disable CA1031
// This is a worker service that is not intended to be instantiated directly, so we suppress the warning.
#pragma warning disable CA1812

namespace Stickerlandia.PrintService.MigrationService;

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
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintServiceDbContext>();

            await RunMigrationAsync(dbContext, cancellationToken);
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

    private static async Task RunMigrationAsync(PrintServiceDbContext dbContext, CancellationToken cancellationToken)
    {
        using var runMigrationActivity = s_activitySource.StartActivity("run.migration", ActivityKind.Client);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }
}