/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Stickerlandia.PrintService.Agnostic.Data;
using Stickerlandia.PrintService.Agnostic.Data.Entities;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Agnostic.Repositories;

public class PostgresOutbox(PrintServiceDbContext dbContext) : IOutbox
{
    public async Task StoreEventFor(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var outboxItem = new OutboxItem
        {
            EventType = domainEvent.EventName,
            EventData = domainEvent.ToJsonString()
        };

        var entity = OutboxItemEntity.FromDomain(outboxItem);

        await dbContext.OutboxItems.AddAsync(entity).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        var entities = await dbContext.OutboxItems
            .Where(o => !o.Processed && !o.Failed)
            .OrderBy(o => o.EventTime)
            .Take(maxCount)
            .ToListAsync()
            .ConfigureAwait(false);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        ArgumentNullException.ThrowIfNull(outboxItem);

        var entity = await dbContext.OutboxItems
            .FirstOrDefaultAsync(o => o.ItemId == outboxItem.ItemId)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.Processed = outboxItem.Processed;
        entity.Failed = outboxItem.Failed;
        entity.FailureReason = outboxItem.FailureReason;

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
