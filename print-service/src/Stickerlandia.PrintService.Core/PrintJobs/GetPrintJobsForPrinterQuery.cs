// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.PrintJobs;

/// <summary>
/// Query to retrieve queued print jobs for a specific printer.
/// </summary>
public record GetPrintJobsForPrinterQuery
{
    /// <summary>
    /// The printer ID to retrieve jobs for.
    /// </summary>
    public string PrinterId { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of jobs to return (default: 10).
    /// </summary>
    public int MaxJobs { get; init; } = 10;
}
