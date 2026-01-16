/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json.Serialization;

#pragma warning disable CA1812

namespace Stickerlandia.PrintService.IntegrationTest.ViewModels;

internal sealed record RegisterPrinterResponse
{
    [JsonPropertyName("printerId")]
    public string PrinterId { get; set; } = string.Empty;

    [JsonPropertyName("EventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("printerName")]
    public string PrinterName { get; set; } = string.Empty;

    [JsonPropertyName("Key")]
    public string Key { get; set; } = string.Empty;
}
