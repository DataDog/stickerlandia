/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.PrintService.Core.Outbox;

public class OutboxItem
{
    public OutboxItem()
    {
        ItemId = Guid.NewGuid().ToString();
        EventTime = DateTime.UtcNow;

        var currentActivity = Activity.Current;

        if (currentActivity != null)
        {
            TraceId = $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01";
        }
    }

    public string ItemId { get; set; }
    public string EventType { get; set; } = "";

    public string EventData { get; set; } = "";

    public DateTime EventTime { get; set; }

    public bool Processed { get; set; }

    public bool Failed { get; set; }

    public string? FailureReason { get; set; }

    public string? TraceId { get; set; }

    public string EmailAddress { get; set; } = "";
}