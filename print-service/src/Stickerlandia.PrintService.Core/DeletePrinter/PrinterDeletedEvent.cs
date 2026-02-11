// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.DeletePrinter;

public record PrinterDeletedEvent : DomainEvent
{
    public PrinterDeletedEvent() { }

    public PrinterDeletedEvent(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer, nameof(printer));

        PrinterId = printer.Id?.Value ?? "";
        PrinterEventName = printer.EventName;
        PrinterName = printer.PrinterName;
    }

    [JsonPropertyName("EventName")]
    public override string EventName => "printers.deleted.v1";

    [JsonPropertyName("eventVersion")]
    public override string EventVersion => "1.0";

    public override string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }

    [JsonPropertyName("printerId")]
    public string PrinterId { get; set; } = "";

    [JsonPropertyName("printerEventName")]
    public string PrinterEventName { get; set; } = "";

    [JsonPropertyName("printerName")]
    public string PrinterName { get; set; } = "";
}
