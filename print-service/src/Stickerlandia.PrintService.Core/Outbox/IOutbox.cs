/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.Outbox;

public interface IOutbox
{
    Task StoreEventFor(DomainEvent domainEvent);

    Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100);

    Task UpdateOutboxItem(OutboxItem outboxItem);
}