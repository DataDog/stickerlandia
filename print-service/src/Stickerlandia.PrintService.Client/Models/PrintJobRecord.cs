// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application models
#pragma warning disable CA1515, CA1056

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Client.Models;

/// <summary>
/// Represents a print job record stored locally.
/// </summary>
public sealed record PrintJobRecord
{
    [JsonPropertyName("printJobId")]
    public string PrintJobId { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; } = string.Empty;

    [JsonPropertyName("stickerUrl")]
    public string StickerUrl { get; init; } = string.Empty;

    [JsonPropertyName("receivedAt")]
    public DateTimeOffset ReceivedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "Received";

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; init; }
}

/// <summary>
/// Represents printer information returned from the backend.
/// </summary>
public sealed record PrinterInfo
{
    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = string.Empty;

    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; init; } = string.Empty;
}

/// <summary>
/// Represents a print job received from the backend.
/// </summary>
public sealed record PrintJobDto
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
}

/// <summary>
/// Response from polling for print jobs.
/// </summary>
public sealed record PollJobsResponse
{
    [JsonPropertyName("jobs")]
    public IReadOnlyList<PrintJobDto> Jobs { get; init; } = [];
}

/// <summary>
/// Response from acknowledging a print job.
/// </summary>
public sealed record AcknowledgeJobResponse
{
    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; init; }
}

/// <summary>
/// Generic API response wrapper.
/// </summary>
public sealed record ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }
}
