// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.AWS;

public class AwsOutboxImplementation : IOutbox
{
    public Task StoreEventFor(DomainEvent domainEvent)
    {
        // Dummy implementation
        return Task.CompletedTask;
    }

    public Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        // Dummy implementation
        return Task.FromResult(new List<OutboxItem>());
    }

    public Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        // Dummy implementation
        return Task.CompletedTask;
    }
}