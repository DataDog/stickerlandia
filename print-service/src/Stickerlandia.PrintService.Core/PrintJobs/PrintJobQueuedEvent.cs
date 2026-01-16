// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.PrintJobs;

public record PrintJobQueuedEvent : DomainEvent
{
    public PrintJobQueuedEvent() { }

    public PrintJobQueuedEvent(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        PrintJobId = printJob.Id.Value;
        PrinterId = printJob.PrinterId.Value;
        UserId = printJob.UserId;
        StickerId = printJob.StickerId;
    }

    [JsonPropertyName("EventName")]
    public override string EventName => "printJobs.queued.v1";

    [JsonPropertyName("eventVersion")]
    public override string EventVersion => "1.0";

    public override string ToJsonString() => JsonSerializer.Serialize(this);

    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; set; } = string.Empty;

    [JsonPropertyName("printerId")]
    public string PrinterId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; set; } = string.Empty;
}
