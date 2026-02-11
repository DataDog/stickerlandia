// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1056

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Data transfer object for print jobs returned to printer clients.
/// </summary>
public record PrintJobDto
{
    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; } = string.Empty;

    [JsonPropertyName("stickerUrl")]
    public string StickerUrl { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("traceparent")]
    public string TraceParent { get; init; } = string.Empty;

    [JsonPropertyName("propagationHeaders")]
    public Dictionary<string, string> PropagationHeaders { get; init; } = new();

    /// <summary>
    /// Creates a DTO from a PrintJob entity.
    /// </summary>
    public static PrintJobDto FromPrintJob(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        return new PrintJobDto
        {
            PrintJobId = printJob.Id.Value,
            UserId = printJob.UserId,
            StickerId = printJob.StickerId,
            StickerUrl = printJob.StickerUrl,
            CreatedAt = printJob.CreatedAt,
            PropagationHeaders = printJob.PropagationHeaders,
            TraceParent = printJob.TraceParent
        };
    }
}
