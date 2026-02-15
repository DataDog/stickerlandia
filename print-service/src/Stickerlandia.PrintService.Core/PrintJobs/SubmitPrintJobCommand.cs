// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1056

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.PrintJobs;

public record SubmitPrintJobCommand
{
    [JsonIgnore]
    public string EventName { get; init; } = string.Empty;

    [JsonIgnore]
    public string PrinterName { get; init; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; } = string.Empty;

    [JsonPropertyName("stickerUrl")]
    public string StickerUrl { get; init; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(UserId)
               && !string.IsNullOrEmpty(StickerId)
               && !string.IsNullOrEmpty(StickerUrl);
    }
}
