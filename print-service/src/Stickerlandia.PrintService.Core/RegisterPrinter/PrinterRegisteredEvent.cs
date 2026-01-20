/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.RegisterPrinter;

public record PrinterRegisteredEvent : DomainEvent
{
    public PrinterRegisteredEvent() { }

    public PrinterRegisteredEvent(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer, nameof(printer));

        PrinterId = printer?.Id?.Value ?? "";
    }

    [JsonPropertyName("EventName")]
    public override string EventName => "printers.registered.v1";

    [JsonPropertyName("eventVersion")]
    public override string EventVersion => "1.0";
    public override string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }

    [JsonPropertyName("printerId")]
    public string PrinterId { get; set; } = "";
}