// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.PrintService.Core.GetPrinters;

/// <summary>
/// Query to get the status of all printers for a specific event.
/// </summary>
public record GetPrinterStatusesQuery
{
    /// <summary>
    /// The event name to get printer statuses for.
    /// </summary>
    public string EventName { get; init; } = string.Empty;
}
