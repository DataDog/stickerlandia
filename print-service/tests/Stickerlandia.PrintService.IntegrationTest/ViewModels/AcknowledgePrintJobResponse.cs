/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// These classes are instantiated via JSON deserialization
#pragma warning disable CA1812

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.IntegrationTest.ViewModels;

internal sealed record AcknowledgePrintJobResponse
{
    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; init; }
}

internal sealed record AcknowledgePrintJobRequest
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }
}
