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

public record RegisterPrinterResponse : PrinterDTO
{
    public RegisterPrinterResponse(Printer printer) : base(printer)
    {
        Key = printer.Key;
    }

    [JsonPropertyName("Key")] public string Key { get; set; } = string.Empty;
}