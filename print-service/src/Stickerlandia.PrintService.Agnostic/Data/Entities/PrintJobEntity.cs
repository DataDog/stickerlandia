/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// URL properties use string for JSON serialization simplicity
#pragma warning disable CA1056

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Agnostic.Data.Entities;

public class PrintJobEntity
{
    public Guid Id { get; set; }

    public string PrintJobId { get; set; } = string.Empty;

    public string PrinterId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string StickerId { get; set; } = string.Empty;

    public string StickerUrl { get; set; } = string.Empty;

    public int Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public uint RowVersion { get; set; }

    public static PrintJobEntity FromDomain(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        return new PrintJobEntity
        {
            Id = Guid.NewGuid(),
            PrintJobId = printJob.Id.Value,
            PrinterId = printJob.PrinterId.Value,
            UserId = printJob.UserId,
            StickerId = printJob.StickerId,
            StickerUrl = printJob.StickerUrl,
            Status = (int)printJob.Status,
            CreatedAt = printJob.CreatedAt,
            ProcessedAt = printJob.ProcessedAt,
            CompletedAt = printJob.CompletedAt,
            FailureReason = printJob.FailureReason
        };
    }

    public PrintJob ToDomain()
    {
        return PrintJob.From(
            new PrintJobId(PrintJobId),
            new PrinterId(PrinterId),
            UserId,
            StickerId,
            StickerUrl,
            (PrintJobStatus)Status,
            CreatedAt,
            ProcessedAt,
            CompletedAt,
            FailureReason);
    }
}
