// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1054
#pragma warning disable CA1056

namespace Stickerlandia.PrintService.Core.PrintJobs;

public record PrintJobId
{
    public string Value { get; init; }

    public PrintJobId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
        Value = value;
    }

    public static PrintJobId NewId() => new(Guid.NewGuid().ToString());
}

public enum PrintJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

public class PrintJob
{
    private readonly List<DomainEvent> _domainEvents;

    private PrintJob()
    {
        _domainEvents = new List<DomainEvent>();
    }

    public static PrintJob Create(
        PrinterId printerId,
        string userId,
        string stickerId,
        string stickerUrl)
    {
        ArgumentNullException.ThrowIfNull(printerId);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(stickerId);
        ArgumentException.ThrowIfNullOrEmpty(stickerUrl);

        var printJob = new PrintJob
        {
            Id = PrintJobId.NewId(),
            PrinterId = printerId,
            UserId = userId,
            StickerId = stickerId,
            StickerUrl = stickerUrl,
            Status = PrintJobStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        printJob._domainEvents.Add(new PrintJobQueuedEvent(printJob));

        return printJob;
    }

    public static PrintJob From(
        PrintJobId id,
        PrinterId printerId,
        string userId,
        string stickerId,
        string stickerUrl,
        PrintJobStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? processedAt,
        DateTimeOffset? completedAt,
        string? failureReason)
    {
        return new PrintJob
        {
            Id = id,
            PrinterId = printerId,
            UserId = userId,
            StickerId = stickerId,
            StickerUrl = stickerUrl,
            Status = status,
            CreatedAt = createdAt,
            ProcessedAt = processedAt,
            CompletedAt = completedAt,
            FailureReason = failureReason
        };
    }

    public void MarkAsProcessing()
    {
        if (Status != PrintJobStatus.Queued)
        {
            throw new InvalidOperationException($"Cannot mark job as processing when status is {Status}");
        }

        Status = PrintJobStatus.Processing;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != PrintJobStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete job when status is {Status}");
        }

        Status = PrintJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        _domainEvents.Add(new PrintJobCompletedEvent(this));
    }

    public void Fail(string reason)
    {
        if (Status != PrintJobStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot fail job when status is {Status}");
        }

        ArgumentException.ThrowIfNullOrEmpty(reason);

        Status = PrintJobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        FailureReason = reason;
        _domainEvents.Add(new PrintJobFailedEvent(this));
    }

    public PrintJobId Id { get; private set; } = null!;

    public PrinterId PrinterId { get; private set; } = null!;

    public string UserId { get; private set; } = string.Empty;

    public string StickerId { get; private set; } = string.Empty;

    public string StickerUrl { get; private set; } = string.Empty;

    public PrintJobStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();
}
