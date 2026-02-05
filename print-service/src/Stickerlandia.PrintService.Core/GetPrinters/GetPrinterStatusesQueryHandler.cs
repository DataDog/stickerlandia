// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.PrintService.Core.GetPrinters;

/// <summary>
/// Handler for retrieving printer statuses for an event.
/// </summary>
public class GetPrinterStatusesQueryHandler(IPrinterRepository repository)
{
    public async Task<GetPrinterStatusesResponse> Handle(GetPrinterStatusesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrEmpty(query.EventName))
        {
            throw new ArgumentException("Event name is required", nameof(query));
        }

        Activity.Current?.AddTag("printer.status.event_name", query.EventName);

        var printers = await repository.GetPrintersForEventAsync(query.EventName);

        var statuses = printers.Select(PrinterStatusDto.FromPrinter).ToList();

        Activity.Current?.AddTag("printer.status.count", statuses.Count);

        return new GetPrinterStatusesResponse
        {
            Printers = statuses
        };
    }
}

/// <summary>
/// Response containing printer statuses.
/// </summary>
public sealed record GetPrinterStatusesResponse
{
    public IReadOnlyList<PrinterStatusDto> Printers { get; init; } = [];
}
