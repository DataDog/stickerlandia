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
}
