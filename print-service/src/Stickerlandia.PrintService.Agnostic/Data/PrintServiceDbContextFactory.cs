/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stickerlandia.PrintService.Agnostic.Data;

/// <summary>
/// Design-time factory for creating PrintServiceDbContext for EF Core migrations.
/// </summary>
public class PrintServiceDbContextFactory : IDesignTimeDbContextFactory<PrintServiceDbContext>
{
    public PrintServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PrintServiceDbContext>();

        // Use a placeholder connection string for design-time operations
        // This will be overridden at runtime with the actual connection string
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=printservice;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("Stickerlandia.PrintService.Agnostic"));
        optionsBuilder.UseOpenIddict();

        return new PrintServiceDbContext(optionsBuilder.Options);
    }
}
