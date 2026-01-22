// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515

using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// Service for storing and retrieving print job records locally.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Stores a received print job.
    /// </summary>
    Task StoreJobAsync(PrintJobDto job);

    /// <summary>
    /// Marks a job as completed.
    /// </summary>
    Task MarkCompletedAsync(string printJobId);

    /// <summary>
    /// Marks a job as failed.
    /// </summary>
    Task MarkFailedAsync(string printJobId, string reason);

    /// <summary>
    /// Marks a job as acknowledged to the backend.
    /// </summary>
    Task MarkAcknowledgedAsync(string printJobId);

    /// <summary>
    /// Gets a job record by ID.
    /// </summary>
    Task<PrintJobRecord?> GetJobAsync(string printJobId);

    /// <summary>
    /// Gets all job records, optionally filtered.
    /// </summary>
    Task<IReadOnlyList<PrintJobRecord>> GetJobsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, string? status = null);

    /// <summary>
    /// Gets jobs that have not been acknowledged to the backend.
    /// </summary>
    Task<IReadOnlyList<PrintJobRecord>> GetUnacknowledgedJobsAsync();

    /// <summary>
    /// Gets the count of jobs processed today.
    /// </summary>
    Task<int> GetJobsProcessedTodayAsync();

    /// <summary>
    /// Gets the total count of jobs processed.
    /// </summary>
    Task<int> GetTotalJobsProcessedAsync();
}
