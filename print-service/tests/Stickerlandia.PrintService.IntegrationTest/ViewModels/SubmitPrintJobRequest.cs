/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1056, CA1812

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.IntegrationTest.ViewModels;

internal sealed record SubmitPrintJobRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("stickerId")]
    public string StickerId { get; init; } = string.Empty;

    [JsonPropertyName("stickerUrl")]
    public string StickerUrl { get; init; } = string.Empty;
}
