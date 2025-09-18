/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Agnostic;

public class UserManagementDbContext : IdentityDbContext<PostgresUserAccount>
{
    public new DbSet<PostgresUserAccount> Users { get; set; } = null!;
    public DbSet<PostgresOutboxItem> OutboxItems { get; set; } = null!;

    public UserManagementDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        base.OnModelCreating(builder);
        
        builder.UseOpenIddict();

        builder.Entity<PostgresUserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.ClaimedStickerCount).HasColumnName("claimed_sticker_count");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.AccountTier).HasColumnName("account_tier");
            entity.Property(e => e.AccountType).HasColumnName("account_type");
        });

        builder.Entity<PostgresOutboxItem>(entity =>
        {
            entity.ToTable("outbox_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmailAddress).HasColumnName("email_address");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.EventData).HasColumnName("event_data");
            entity.Property(e => e.EventTime).HasColumnName("event_time");
            entity.Property(e => e.Processed).HasColumnName("processed");
            entity.Property(e => e.Failed).HasColumnName("failed");
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
        });
    }
}