// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Core.PrintJobs;

public class SubmitPrintJobCommandHandler(
    IOutbox outbox,
    IPrinterRepository printerRepository,
    IPrintJobRepository printJobRepository,
    PrintJobInstrumentation instrumentation)
{
    public async Task<SubmitPrintJobResponse> Handle(
        string eventName,
        string printerName,
        SubmitPrintJobCommand command)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(printerName);
        ArgumentNullException.ThrowIfNull(command);

        if (!command.IsValid())
        {
            throw new InvalidPrintJobException("Invalid print job command. UserId, StickerId, and a valid StickerUrl are required.");
        }

        using var activity = PrintJobInstrumentation.StartSubmitJobActivity(eventName, printerName, command.StickerId);

        try
        {
            var printer = await printerRepository.GetPrinterAsync(eventName, printerName);

            if (printer is null)
            {
                throw new PrinterNotFoundException($"Printer '{printerName}' not found for event '{eventName}'.");
            }

            var printJob = PrintJob.Create(
                printer.Id!,
                command.UserId,
                command.StickerId,
                command.StickerUrl);

            await printJobRepository.AddAsync(printJob);

            foreach (var domainEvent in printJob.DomainEvents)
            {
                await outbox.StoreEventFor(domainEvent);
            }

            activity?.SetTag("print.job_id", printJob.Id.Value);
            activity?.SetTag("print.printer_id", printer.Id!.Value);
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Record metrics
            instrumentation.RecordJobSubmitted(printer.Id!.Value, eventName);

            return new SubmitPrintJobResponse(printJob);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}
