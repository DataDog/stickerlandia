/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stickerlandia.PrintService.Agnostic.Data.Entities;

namespace Stickerlandia.PrintService.Agnostic.Data.Configurations;

public class OutboxItemEntityConfiguration : IEntityTypeConfiguration<OutboxItemEntity>
{
    public void Configure(EntityTypeBuilder<OutboxItemEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxItems");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.ItemId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.EventData)
            .IsRequired();

        builder.Property(o => o.FailureReason)
            .HasMaxLength(1024);

        builder.Property(o => o.TraceId)
            .HasMaxLength(256);

        builder.Property(o => o.EmailAddress)
            .HasMaxLength(256);

        // Unique index on ItemId
        builder.HasIndex(o => o.ItemId)
            .IsUnique();

        // Index for GetUnprocessedItemsAsync
        builder.HasIndex(o => new { o.Processed, o.EventTime });
    }
}
