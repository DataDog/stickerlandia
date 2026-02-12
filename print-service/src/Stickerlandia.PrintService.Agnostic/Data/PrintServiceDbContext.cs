/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Stickerlandia.PrintService.Agnostic.Data.Configurations;
using Stickerlandia.PrintService.Agnostic.Data.Entities;

namespace Stickerlandia.PrintService.Agnostic.Data;

public class PrintServiceDbContext(DbContextOptions<PrintServiceDbContext> options) : DbContext(options)
{
    public DbSet<PrinterEntity> Printers { get; set; } = null!;

    public DbSet<PrintJobEntity> PrintJobs { get; set; } = null!;

    public DbSet<OutboxItemEntity> OutboxItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PrinterEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PrintJobEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxItemEntityConfiguration());
    }
}
