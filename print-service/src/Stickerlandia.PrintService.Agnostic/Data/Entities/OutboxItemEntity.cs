/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Agnostic.Data.Entities;

public class OutboxItemEntity
{
    public Guid Id { get; set; }

    public string ItemId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string EventData { get; set; } = string.Empty;

    public DateTime EventTime { get; set; }

    public bool Processed { get; set; }

    public bool Failed { get; set; }

    public string? FailureReason { get; set; }

    public string? TraceId { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public static OutboxItemEntity FromDomain(OutboxItem outboxItem)
    {
        ArgumentNullException.ThrowIfNull(outboxItem);

        return new OutboxItemEntity
        {
            Id = Guid.NewGuid(),
            ItemId = outboxItem.ItemId,
            EventType = outboxItem.EventType,
            EventData = outboxItem.EventData,
            EventTime = outboxItem.EventTime,
            Processed = outboxItem.Processed,
            Failed = outboxItem.Failed,
            FailureReason = outboxItem.FailureReason,
            TraceId = outboxItem.TraceId,
            EmailAddress = outboxItem.EmailAddress
        };
    }

    public OutboxItem ToDomain()
    {
        return new OutboxItem
        {
            ItemId = ItemId,
            EventType = EventType,
            EventData = EventData,
            EventTime = EventTime,
            Processed = Processed,
            Failed = Failed,
            FailureReason = FailureReason,
            TraceId = TraceId,
            EmailAddress = EmailAddress
        };
    }
}
