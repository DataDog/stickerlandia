// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Collections.ObjectModel;
using Amazon.EventBridge.Model;

namespace Stickerlandia.PrintService.AWS;

public class EventBridgePartialFailureException : Exception
{
    public ReadOnlyCollection<PutEventsResultEntry> FailedEntries { get; }

    public EventBridgePartialFailureException()
        : base("EventBridge PutEvents partial failure.")
    {
        FailedEntries = new ReadOnlyCollection<PutEventsResultEntry>([]);
    }

    public EventBridgePartialFailureException(string message)
        : base(message)
    {
        FailedEntries = new ReadOnlyCollection<PutEventsResultEntry>([]);
    }

    public EventBridgePartialFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailedEntries = new ReadOnlyCollection<PutEventsResultEntry>([]);
    }

    public EventBridgePartialFailureException(int failedCount, IList<PutEventsResultEntry> failedEntries)
        : base($"EventBridge PutEvents partial failure: {failedCount} entries failed. " +
               $"Error codes: {string.Join(", ", failedEntries.Select(e => $"{e.ErrorCode}: {e.ErrorMessage}"))}")
    {
        FailedEntries = new ReadOnlyCollection<PutEventsResultEntry>(failedEntries);
    }
}
