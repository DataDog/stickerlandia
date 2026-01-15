// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json.Serialization;

namespace Stickerlandia.PrintService.Core.GetPrinters;

/// <summary>
/// Data transfer object for printer status information.
/// </summary>
public sealed record PrinterStatusDto
{
    /// <summary>
    /// The unique identifier of the printer.
    /// </summary>
    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = string.Empty;

    /// <summary>
    /// The name of the printer.
    /// </summary>
    [JsonPropertyName("printerName")]
    public string PrinterName { get; init; } = string.Empty;

    /// <summary>
    /// The online/offline status of the printer.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// The last time this printer sent a heartbeat.
    /// </summary>
    [JsonPropertyName("lastHeartbeat")]
    public DateTimeOffset? LastHeartbeat { get; init; }

    /// <summary>
    /// The last time this printer processed a job.
    /// </summary>
    [JsonPropertyName("lastJobProcessed")]
    public DateTimeOffset? LastJobProcessed { get; init; }

    /// <summary>
    /// Creates a PrinterStatusDto from a Printer entity.
    /// </summary>
    public static PrinterStatusDto FromPrinter(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        return new PrinterStatusDto
        {
            PrinterId = printer.Id?.Value ?? string.Empty,
            PrinterName = printer.PrinterName,
            Status = printer.Status.ToString(),
            LastHeartbeat = printer.LastHeartbeat,
            LastJobProcessed = printer.LastJobProcessed
        };
    }
}
