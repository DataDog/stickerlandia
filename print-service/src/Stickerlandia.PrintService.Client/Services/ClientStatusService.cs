// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1515, CA1003, CA1024

using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// Shared state service for the printer client, used by both UI and background services.
/// </summary>
public sealed class ClientStatusService
{
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when any status changes.
    /// </summary>
    public event Action? OnStatusChanged;

    /// <summary>
    /// Current connection status.
    /// </summary>
    public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.NotConfigured;

    /// <summary>
    /// Last error message, if any.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Time of the last successful poll.
    /// </summary>
    public DateTimeOffset? LastPollTime { get; private set; }

    /// <summary>
    /// Number of jobs retrieved in the last poll.
    /// </summary>
    public int LastPollJobCount { get; private set; }

    /// <summary>
    /// Number of jobs processed today.
    /// </summary>
    public int JobsProcessedToday { get; private set; }

    /// <summary>
    /// Total number of jobs processed.
    /// </summary>
    public int JobsProcessedTotal { get; private set; }

    /// <summary>
    /// Information about the connected printer.
    /// </summary>
    public PrinterInfo? PrinterInfo { get; private set; }

    /// <summary>
    /// Updates the connection status.
    /// </summary>
    public void UpdateConnectionStatus(ConnectionStatus status, string? error = null)
    {
        lock (_lock)
        {
            ConnectionStatus = status;
            LastError = error;
        }
        NotifyStatusChanged();
    }

    /// <summary>
    /// Updates printer information.
    /// </summary>
    public void UpdatePrinterInfo(PrinterInfo? info)
    {
        lock (_lock)
        {
            PrinterInfo = info;
        }
        NotifyStatusChanged();
    }

    /// <summary>
    /// Updates the last poll information.
    /// </summary>
    public void UpdateLastPoll(DateTimeOffset time, int jobCount)
    {
        lock (_lock)
        {
            LastPollTime = time;
            LastPollJobCount = jobCount;
        }
        NotifyStatusChanged();
    }

    /// <summary>
    /// Updates job counts.
    /// </summary>
    public void UpdateJobCounts(int today, int total)
    {
        lock (_lock)
        {
            JobsProcessedToday = today;
            JobsProcessedTotal = total;
        }
        NotifyStatusChanged();
    }

    /// <summary>
    /// Gets a human-readable status string.
    /// </summary>
    public string GetStatusText() => ConnectionStatus switch
    {
        ConnectionStatus.NotConfigured => "Not Configured",
        ConnectionStatus.Connected => "Connected",
        ConnectionStatus.Disconnected => "Disconnected",
        ConnectionStatus.AuthenticationFailed => "Authentication Failed",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets a CSS class for the status indicator.
    /// </summary>
    public string GetStatusCssClass() => ConnectionStatus switch
    {
        ConnectionStatus.Connected => "status-online",
        ConnectionStatus.NotConfigured => "status-warning",
        _ => "status-offline"
    };

    private void NotifyStatusChanged()
    {
        OnStatusChanged?.Invoke();
    }
}
