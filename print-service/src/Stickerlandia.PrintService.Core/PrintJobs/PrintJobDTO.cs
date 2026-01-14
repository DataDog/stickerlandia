// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1056

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.PrintJobs;

public record PrintJobDTO
{
    public PrintJobDTO() { }

    public PrintJobDTO(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        PrintJobId = printJob.Id.Value;
        PrinterId = printJob.PrinterId.Value;
        UserId = printJob.UserId;
        StickerId = printJob.StickerId;
        StickerUrl = printJob.StickerUrl;
        Status = printJob.Status.ToString();
        CreatedAt = printJob.CreatedAt;
        ProcessedAt = printJob.ProcessedAt;
        CompletedAt = printJob.CompletedAt;
    }

    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; init; } = string.Empty;

    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; } = string.Empty;

    [JsonPropertyName("stickerUrl")]
    public string StickerUrl { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("processedAt")]
    public DateTimeOffset? ProcessedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }
}
