/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.EntityFrameworkCore;
using Stickerlandia.PrintService.Agnostic.Data;
using Stickerlandia.PrintService.Agnostic.Data.Entities;
using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.Agnostic.Repositories;

public class PostgresPrinterRepository(PrintServiceDbContext dbContext) : IPrinterRepository
{
    public async Task AddPrinterAsync(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printer.Id);

        var entity = PrinterEntity.FromDomain(printer);
        await dbContext.Printers.AddAsync(entity).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<Printer?> GetPrinterByIdAsync(Guid printerId)
    {
        var printerIdString = printerId.ToString();

        var entity = await dbContext.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PrinterId == printerIdString)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<Printer?> GetPrinterAsync(string eventName, string printerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(printerName);

        var printerId = $"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}";

        var entity = await dbContext.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PrinterId == printerId)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<Printer?> GetPrinterByKeyAsync(string apiKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        // Direct index lookup on Key column (replaces GSI1 query)
        var entity = await dbContext.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == apiKey)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<List<Printer>> GetPrintersForEventAsync(string eventName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        var entities = await dbContext.Printers
            .AsNoTracking()
            .Where(p => p.EventName == eventName)
            .ToListAsync()
            .ConfigureAwait(false);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<bool> PrinterExistsAsync(string eventName, string printerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(printerName);

        var printerId = $"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}";

        return await dbContext.Printers
            .AsNoTracking()
            .AnyAsync(p => p.PrinterId == printerId)
            .ConfigureAwait(false);
    }

    public async Task UpdateHeartbeatAsync(string printerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        // Use ExecuteUpdateAsync for efficient single-column update
        await dbContext.Printers
            .Where(p => p.PrinterId == printerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.LastHeartbeat, DateTimeOffset.UtcNow))
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printer.Id);

        var entity = await dbContext.Printers
            .FirstOrDefaultAsync(p => p.PrinterId == printer.Id.Value)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.EventName = printer.EventName;
        entity.PrinterName = printer.PrinterName;
        entity.Key = printer.Key;
        entity.LastHeartbeat = printer.LastHeartbeat;
        entity.LastJobProcessed = printer.LastJobProcessed;

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
