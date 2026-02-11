/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// These classes are instantiated via JSON deserialization
#pragma warning disable CA1812

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.IntegrationTest.ViewModels;

internal sealed record GetPrinterStatusesResponse
{
    [JsonPropertyName("printers")]
    public IReadOnlyList<PrinterStatusDto> Printers { get; init; } = [];
}

internal sealed record PrinterStatusDto
{
    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = string.Empty;

    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("lastHeartbeat")]
    public DateTimeOffset? LastHeartbeat { get; init; }

    [JsonPropertyName("lastJobProcessed")]
    public DateTimeOffset? LastJobProcessed { get; init; }
}
