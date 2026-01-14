// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Handler for retrieving and claiming print jobs for a printer.
/// This handler atomically retrieves queued jobs and marks them as Processing.
/// </summary>
public class GetPrintJobsForPrinterQueryHandler(
    IPrintJobRepository printJobRepository,
    IPrinterRepository printerRepository)
{
    /// <summary>
    /// Retrieves queued jobs for the printer and marks them as Processing.
    /// Also updates the printer's heartbeat timestamp.
    /// </summary>
    public async Task<GetPrintJobsForPrinterResponse> Handle(GetPrintJobsForPrinterQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrEmpty(query.PrinterId);

        // Update printer heartbeat
        await printerRepository.UpdateHeartbeatAsync(query.PrinterId);

        // Get queued jobs and atomically claim them
        var jobs = await printJobRepository.GetQueuedJobsForPrinterAsync(
            query.PrinterId,
            query.MaxJobs);

        Activity.Current?.AddTag("printjob.poll.printer_id", query.PrinterId);
        Activity.Current?.AddTag("printjob.poll.jobs_returned", jobs.Count);

        return new GetPrintJobsForPrinterResponse
        {
            Jobs = jobs.Select(PrintJobDto.FromPrintJob).ToList()
        };
    }
}

/// <summary>
/// Response containing print jobs for a printer.
/// </summary>
public record GetPrintJobsForPrinterResponse
{
    public IReadOnlyList<PrintJobDto> Jobs { get; init; } = [];
}
