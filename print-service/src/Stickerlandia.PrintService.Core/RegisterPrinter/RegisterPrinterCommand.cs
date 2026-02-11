/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.RegisterPrinter;

public record RegisterPrinterCommand
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("printerName")]
    public string PrinterName { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(EventName) && !string.IsNullOrEmpty(PrinterName);
    }
}
