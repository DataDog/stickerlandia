// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Core.GetPrinters;

/// <summary>
/// Handler for retrieving printer statuses for an event.
/// </summary>
public class GetPrinterStatusesQueryHandler(IPrinterRepository repository, IPrintJobRepository printJobRepository, PrintJobInstrumentation instrumentation)
{
    public async Task<GetPrinterStatusesResponse> Handle(GetPrinterStatusesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrEmpty(query.EventName))
        {
            throw new ArgumentException("Event name is required", nameof(query));
        }

        using var activity = PrintJobInstrumentation.StartGetPrinterStatusesActivity(query.EventName);

        try
        {
            var printers = await repository.GetPrintersForEventAsync(query.EventName);

            var statuses = new List<PrinterStatusDto>(printers.Count);
            foreach (var p in printers)
            {
                var count = await printJobRepository.CountActiveJobsForPrinterAsync(p.Id!.Value);
                statuses.Add(PrinterStatusDto.FromPrinter(p, count));
            }

            var onlineCount = statuses.Count(s => s.Status == "Online");
            var offlineCount = statuses.Count - onlineCount;

            activity?.SetTag("print.total_printers", statuses.Count);
            activity?.SetTag("print.online_count", onlineCount);
            activity?.SetTag("print.offline_count", offlineCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            instrumentation.RecordPrinterStatusCheck(query.EventName, onlineCount, offlineCount);

            return new GetPrinterStatusesResponse
            {
                Printers = statuses
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}

/// <summary>
/// Response containing printer statuses.
/// </summary>
public sealed record GetPrinterStatusesResponse
{
    public IReadOnlyList<PrinterStatusDto> Printers { get; init; } = [];
}
