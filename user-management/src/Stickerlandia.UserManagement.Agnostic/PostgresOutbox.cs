/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public class PostgresOutbox(
    UserManagementDbContext dbContext,
    ILogger<PostgresOutbox> logger)
    : IOutbox
{
    public async Task StoreEventFor(string accountId, DomainEvent domainEvent)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId, nameof(accountId));
        ArgumentNullException.ThrowIfNull(domainEvent, nameof(domainEvent));
        
        var outboxItem = new PostgresOutboxItem
        {
            Id = Guid.NewGuid().ToString(),
            EventType = domainEvent.EventName,
            EventData = domainEvent.ToJsonString(),
            EmailAddress = accountId,
            EventTime = DateTime.UtcNow,
            Processed = false,
            Failed = false
        };

        await dbContext.OutboxItems.AddAsync(outboxItem);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        try
        {
            var items = await dbContext.OutboxItems
                .Where(o => o.Processed == false && o.Failed == false)
                .Take(maxCount)
                .ToListAsync();

            return items.Select(item => new OutboxItem
            {
                ItemId = item.Id,
                EmailAddress = item.EmailAddress,
                EventType = item.EventType,
                EventData = item.EventData,
                EventTime = item.EventTime,
                Processed = item.Processed,
                Failed = item.Failed,
                FailureReason = item.FailureReason,
                TraceId = item.TraceId
            }).ToList();
        }
        catch (Exception ex)
        {
            Log.UnknownException(logger, ex);
            throw new DatabaseFailureException("Error retrieving unprocessed outbox items", ex);
        }
    }

    public async Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(outboxItem, nameof(outboxItem));
            var item = await dbContext.OutboxItems.FindAsync(outboxItem.ItemId);

            if (item == null) throw new DatabaseFailureException($"Outbox item with ID {outboxItem.ItemId} not found");

            item.Processed = outboxItem.Processed;
            item.Failed = outboxItem.Failed;
            item.FailureReason = outboxItem.FailureReason;
            item.TraceId = outboxItem.TraceId;

            dbContext.OutboxItems.Update(item);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseFailureException($"Failed to update outbox item with ID {outboxItem?.ItemId}", ex);
        }
    }
}