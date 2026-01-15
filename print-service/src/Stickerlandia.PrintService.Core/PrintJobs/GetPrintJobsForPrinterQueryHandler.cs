// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Handler for retrieving and claiming print jobs for a printer.
/// This handler atomically retrieves queued jobs and marks them as Processing.
/// </summary>
public class GetPrintJobsForPrinterQueryHandler(
    IPrintJobRepository printJobRepository,
    IPrinterRepository printerRepository,
    PrintJobInstrumentation instrumentation)
{
    /// <summary>
    /// Retrieves queued jobs for the printer and marks them as Processing.
    /// Also updates the printer's heartbeat timestamp.
    /// </summary>
    public async Task<GetPrintJobsForPrinterResponse> Handle(GetPrintJobsForPrinterQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrEmpty(query.PrinterId);

        using var activity = PrintJobInstrumentation.StartPollJobsActivity(query.PrinterId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Update printer heartbeat
            await printerRepository.UpdateHeartbeatAsync(query.PrinterId);

            // Get queued jobs and atomically claim them
            var jobs = await printJobRepository.GetQueuedJobsForPrinterAsync(
                query.PrinterId,
                query.MaxJobs);

            stopwatch.Stop();

            activity?.SetTag("print.jobs_returned", jobs.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Record metrics
            instrumentation.RecordJobsPolled(query.PrinterId, jobs.Count, stopwatch.Elapsed);

            return new GetPrintJobsForPrinterResponse
            {
                Jobs = jobs.Select(PrintJobDto.FromPrintJob).ToList()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            // Record poll with 0 jobs on error
            instrumentation.RecordJobsPolled(query.PrinterId, 0, stopwatch.Elapsed);

            throw;
        }
    }
}

/// <summary>
/// Response containing print jobs for a printer.
/// </summary>
public record GetPrintJobsForPrinterResponse
{
    public IReadOnlyList<PrintJobDto> Jobs { get; init; } = [];
}
