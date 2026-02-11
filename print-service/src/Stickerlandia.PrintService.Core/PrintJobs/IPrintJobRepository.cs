// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.PrintJobs;

public interface IPrintJobRepository
{
    Task<PrintJob> AddAsync(PrintJob printJob);

    Task<PrintJob?> GetByIdAsync(string printJobId);

    Task<List<PrintJob>> GetQueuedJobsForPrinterAsync(string printerId, int maxJobs = 10);

    Task UpdateAsync(PrintJob printJob);

    /// <summary>
    /// Deletes all print jobs for a given printer.
    /// </summary>
    Task DeleteJobsForPrinterAsync(string printerId);

    /// <summary>
    /// Checks if a printer has any jobs in the given status.
    /// </summary>
    Task<bool> HasJobsInStatusAsync(string printerId, PrintJobStatus status);

    /// <summary>
    /// Counts the number of active (queued or processing) jobs for a printer.
    /// </summary>
    Task<int> CountActiveJobsForPrinterAsync(string printerId);
}
