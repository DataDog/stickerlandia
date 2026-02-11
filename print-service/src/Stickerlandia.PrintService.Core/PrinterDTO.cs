/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core;

public record PrinterDTO
{
    public PrinterDTO(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        PrinterId = printer.Id!.Value;
        EventName = printer.EventName;
        PrinterName = printer.PrinterName;
    }

    [JsonPropertyName("printerId")]
    public string PrinterId { get; set; }

    [JsonPropertyName("EventName")]
    public string EventName { get; set; }

    [JsonPropertyName("printerName")]
    public string PrinterName { get; set; }
}