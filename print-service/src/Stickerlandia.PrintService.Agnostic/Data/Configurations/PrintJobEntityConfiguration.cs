/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stickerlandia.PrintService.Agnostic.Data.Entities;

namespace Stickerlandia.PrintService.Agnostic.Data.Configurations;

public class PrintJobEntityConfiguration : IEntityTypeConfiguration<PrintJobEntity>
{
    public void Configure(EntityTypeBuilder<PrintJobEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PrintJobs");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PrintJobId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.PrinterId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.StickerId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.StickerUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(1024);

        builder.Property(p => p.TraceParent)
            .HasMaxLength(256)
            .HasDefaultValue(string.Empty);

        builder.Property(p => p.PropagationHeadersJson)
            .HasMaxLength(4096)
            .HasDefaultValue("{}");

        // Unique index on PrintJobId
        builder.HasIndex(p => p.PrintJobId)
            .IsUnique();

        // Composite index for GetQueuedJobsForPrinterAsync
        // Replaces DynamoDB GSI1 (PRINTER#{printerId}#STATUS#{status})
        builder.HasIndex(p => new { p.PrinterId, p.Status, p.CreatedAt });

        // Concurrency token using xmin system column in PostgreSQL
        builder.Property(p => p.RowVersion)
            .IsRowVersion();
    }
}
