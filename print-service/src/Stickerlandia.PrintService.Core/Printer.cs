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
        string key)
    {
        return new Printer
        {
            Id = id,
            EventName = eventName,
            PrinterName = printerName,
            Key = key
        };
    }

    public PrinterId? Id { get; private set; }

    public string EventName { get; private set; } = string.Empty;

    public string PrinterName { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;
    
    internal bool Changed { get; private set; }
}