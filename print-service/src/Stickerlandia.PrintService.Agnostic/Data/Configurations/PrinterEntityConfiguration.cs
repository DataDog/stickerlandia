/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stickerlandia.PrintService.Agnostic.Data.Entities;

namespace Stickerlandia.PrintService.Agnostic.Data.Configurations;

public class PrinterEntityConfiguration : IEntityTypeConfiguration<PrinterEntity>
{
    public void Configure(EntityTypeBuilder<PrinterEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Printers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PrinterId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.EventName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.PrinterName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Key)
            .IsRequired()
            .HasMaxLength(64);

        // Unique constraint on PrinterId (the composite key string)
        builder.HasIndex(p => p.PrinterId)
            .IsUnique();

        // Index for GetPrintersForEventAsync - queries by EventName
        builder.HasIndex(p => p.EventName);

        // Unique index for API key lookups (GetPrinterByKeyAsync, ValidateKeyAsync)
        // This replaces DynamoDB GSI1
        builder.HasIndex(p => p.Key)
            .IsUnique();

        // Concurrency token using xmin system column in PostgreSQL
        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
