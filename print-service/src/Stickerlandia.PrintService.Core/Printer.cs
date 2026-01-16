/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Core;

/// <summary>
/// Represents the online/offline status of a printer.
/// </summary>
public enum PrinterStatus
{
    /// <summary>
    /// Printer has sent a heartbeat within the last 2 minutes.
    /// </summary>
    Online,

    /// <summary>
    /// No heartbeat received for more than 2 minutes.
    /// </summary>
    Offline
}

public record PrinterId
{
    public string Value { get; init; }

    public PrinterId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        Value = value;
    }
}

public class Printer
{
    private readonly List<DomainEvent> _domainEvents;

    public Printer()
    {
        _domainEvents = new List<DomainEvent>();
    }

    public static Printer Register(string eventName, string printerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(eventName);
        ArgumentNullException.ThrowIfNullOrEmpty(printerName);
        
        var userAccount = new Printer
        {
            Id = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}"),
            EventName = eventName,
            PrinterName = printerName,
            Key = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        };

        userAccount._domainEvents.Add(new PrinterRegisteredEvent(userAccount));

        return userAccount;
    }

    public static Printer From(
        PrinterId id,
        string eventName,
        string printerName,
        string key,
        DateTimeOffset? lastHeartbeat = null,
        DateTimeOffset? lastJobProcessed = null)
    {
        return new Printer
        {
            Id = id,
            EventName = eventName,
            PrinterName = printerName,
            Key = key,
            LastHeartbeat = lastHeartbeat,
            LastJobProcessed = lastJobProcessed
        };
    }

    public PrinterId? Id { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public string PrinterName { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// The last time this printer sent a heartbeat (via polling).
    /// </summary>
    public DateTimeOffset? LastHeartbeat { get; private set; }

    /// <summary>
    /// The last time this printer processed a job.
    /// </summary>
    public DateTimeOffset? LastJobProcessed { get; private set; }

    /// <summary>
    /// Gets the computed status based on heartbeat timestamp.
    /// Online if heartbeat within last 2 minutes, otherwise Offline.
    /// </summary>
    public PrinterStatus Status
    {
        get
        {
            if (!LastHeartbeat.HasValue)
            {
                return PrinterStatus.Offline;
            }

            var timeSinceHeartbeat = DateTimeOffset.UtcNow - LastHeartbeat.Value;
            return timeSinceHeartbeat.TotalMinutes <= 2 ? PrinterStatus.Online : PrinterStatus.Offline;
        }
    }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    internal bool Changed { get; private set; }

    /// <summary>
    /// Updates the heartbeat timestamp to indicate the printer is active.
    /// </summary>
    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTimeOffset.UtcNow;
        Changed = true;
    }

    /// <summary>
    /// Records that a job was processed.
    /// </summary>
    public void RecordJobProcessed()
    {
        LastJobProcessed = DateTimeOffset.UtcNow;
        Changed = true;
    }
}