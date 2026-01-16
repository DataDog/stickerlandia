/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Stickerlandia.PrintService.Agnostic.Data;
using Stickerlandia.PrintService.Agnostic.Data.Entities;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Agnostic.Repositories;

public class PostgresPrintJobRepository(PrintServiceDbContext dbContext) : IPrintJobRepository
{
    public async Task<PrintJob> AddAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var entity = PrintJobEntity.FromDomain(printJob);
        await dbContext.PrintJobs.AddAsync(entity).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return printJob;
    }

    public async Task<PrintJob?> GetByIdAsync(string printJobId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        var entity = await dbContext.PrintJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.PrintJobId == printJobId)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<List<PrintJob>> GetQueuedJobsForPrinterAsync(string printerId, int maxJobs = 10)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        var claimedJobs = new List<PrintJob>();

        // Query candidates using the composite index
        // (PrinterId, Status, CreatedAt) with Status = Queued
        var candidates = await dbContext.PrintJobs
            .Where(j => j.PrinterId == printerId && j.Status == (int)PrintJobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .Take(maxJobs)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var candidate in candidates)
        {
            // Convert to domain BEFORE claiming (status is still Queued)
            var job = candidate.ToDomain();

            // Attempt atomic claim with optimistic concurrency
            if (await TryClaimJobAsync(candidate).ConfigureAwait(false))
            {
                job.MarkAsProcessing();
                claimedJobs.Add(job);
            }
        }

        return claimedJobs;
    }

    public async Task UpdateAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var entity = await dbContext.PrintJobs
            .FirstOrDefaultAsync(j => j.PrintJobId == printJob.Id.Value)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.PrinterId = printJob.PrinterId.Value;
        entity.UserId = printJob.UserId;
        entity.StickerId = printJob.StickerId;
        entity.StickerUrl = printJob.StickerUrl;
        entity.Status = (int)printJob.Status;
        entity.CreatedAt = printJob.CreatedAt;
        entity.ProcessedAt = printJob.ProcessedAt;
        entity.CompletedAt = printJob.CompletedAt;
        entity.FailureReason = printJob.FailureReason;

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<bool> TryClaimJobAsync(PrintJobEntity job)
    {
        try
        {
            // Transition from Queued to Processing
            job.Status = (int)PrintJobStatus.Processing;
            job.ProcessedAt = DateTimeOffset.UtcNow;

            // EF Core will include RowVersion in WHERE clause for optimistic concurrency
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another process claimed this job - reset entity state
            dbContext.Entry(job).State = EntityState.Unchanged;
            return false;
        }
    }
}
