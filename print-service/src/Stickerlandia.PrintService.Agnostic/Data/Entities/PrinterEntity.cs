/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.Agnostic.Data.Entities;

public class PrinterEntity
{
    public Guid Id { get; set; }

    public string PrinterId { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public string PrinterName { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public DateTimeOffset? LastHeartbeat { get; set; }

    public DateTimeOffset? LastJobProcessed { get; set; }

    public uint RowVersion { get; set; }

    public static PrinterEntity FromDomain(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printer.Id);

        return new PrinterEntity
        {
            Id = Guid.NewGuid(),
            PrinterId = printer.Id.Value,
            EventName = printer.EventName,
            PrinterName = printer.PrinterName,
            Key = printer.Key,
            LastHeartbeat = printer.LastHeartbeat,
            LastJobProcessed = printer.LastJobProcessed
        };
    }

    public Printer ToDomain()
    {
        return Printer.From(
            new PrinterId(PrinterId),
            EventName,
            PrinterName,
            Key,
            LastHeartbeat,
            LastJobProcessed);
    }
}
