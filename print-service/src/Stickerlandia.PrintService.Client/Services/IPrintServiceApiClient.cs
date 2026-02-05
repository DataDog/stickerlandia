// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515

using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// Client for communicating with the Print Service backend API.
/// </summary>
public interface IPrintServiceApiClient
{
    /// <summary>
    /// Polls for available print jobs.
    /// </summary>
    /// <param name="maxJobs">Maximum number of jobs to retrieve.</param>
    /// <returns>List of print jobs to process.</returns>
    Task<IReadOnlyList<PrintJobDto>> PollJobsAsync(int maxJobs = 10);

    /// <summary>
    /// Acknowledges a print job as completed or failed.
    /// </summary>
    /// <param name="printJobId">The ID of the job to acknowledge.</param>
    /// <param name="success">Whether the job completed successfully.</param>
    /// <param name="failureReason">Reason for failure if success is false.</param>
    /// <returns>True if acknowledgment was successful.</returns>
    Task<bool> AcknowledgeJobAsync(string printJobId, bool success, string? failureReason = null);

    /// <summary>
    /// Validates the connection and API key with the backend.
    /// </summary>
    /// <returns>Printer information if valid, null otherwise.</returns>
    Task<PrinterInfo?> ValidateConnectionAsync();
}
