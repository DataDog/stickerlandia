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
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;

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
            await SeedDataAsync(scope.ServiceProvider.GetRequiredService<IPrinterRepository>());
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

    private static async Task SeedDataAsync(IPrinterRepository repository)
    {
        using var seedDataActivity = s_activitySource.StartActivity("run.seedData", ActivityKind.Client);
        
        // Data already seeded
        if (await repository.GetPrinterAsync("default", "default") is not null)
        {
            return;
        }

        var printer = Printer.Register("default", "default");
        printer.UpdateKey("thisisadefaultkey");

        try
        {
            await repository.AddPrinterAsync(printer);
        }
        catch (Exception)
        {
            // Ignore exceptions during seeding as the printer might already exist if the seeding is retried after a failure.
        }
    }
}