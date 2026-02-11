// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

namespace Stickerlandia.PrintService.Core;

public interface IPrinterRepository
{
    Task AddPrinterAsync(Printer printer);

    Task<Printer?> GetPrinterByIdAsync(Guid printerId);

    Task<Printer?> GetPrinterAsync(string eventName, string printerName);

    Task<Printer?> GetPrinterByKeyAsync(string apiKey);

    Task<List<Printer>> GetPrintersForEventAsync(string eventName);

    Task<List<string>> GetDistinctEventNamesAsync();

    Task<bool> PrinterExistsAsync(string eventName, string printerName);

    /// <summary>
    /// Updates the heartbeat timestamp for a printer.
    /// </summary>
    Task UpdateHeartbeatAsync(string printerId);

    /// <summary>
    /// Updates the printer after changes have been made.
    /// </summary>
    Task UpdateAsync(Printer printer);

    /// <summary>
    /// Deletes a printer by event name and printer name.
    /// </summary>
    Task DeleteAsync(string eventName, string printerName);
}