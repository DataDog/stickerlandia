// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Core.DeletePrinter;

public record DeleteEventResponse(int PrintersDeleted);

public class DeleteEventCommandHandler(
    IOutbox outbox,
    IPrinterRepository printerRepository,
    IPrintJobRepository printJobRepository)
{
    public async Task<DeleteEventResponse> Handle(DeleteEventCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(DeleteEventCommand));

        using var activity = PrintJobInstrumentation.StartDeleteEventActivity(command.EventName);

        try
        {
            var printers = await printerRepository.GetPrintersForEventAsync(command.EventName);

            if (printers.Count == 0)
            {
                throw new KeyNotFoundException($"No printers found for event '{command.EventName}'.");
            }

            // Pre-check: if not forcing, verify no printer has in-flight jobs before deleting any
            if (!command.Force)
            {
                foreach (var printer in printers)
                {
                    var hasProcessingJobs = await printJobRepository.HasJobsInStatusAsync(
                        printer.Id!.Value, PrintJobStatus.Processing);

                    if (hasProcessingJobs)
                    {
                        throw new PrinterHasActiveJobsException(
                            $"Printer '{printer.PrinterName}' in event '{command.EventName}' has jobs currently being processed. Use force=true to delete anyway.");
                    }
                }
            }

            // Delete printers one-by-one
            foreach (var printer in printers)
            {
                await printJobRepository.DeleteJobsForPrinterAsync(printer.Id!.Value);
                await printerRepository.DeleteAsync(printer.EventName, printer.PrinterName);

                await outbox.StoreEventFor(new PrinterDeletedEvent(printer));
            }

            activity?.SetTag("print.event_name", command.EventName);
            activity?.SetTag("print.printers_deleted", printers.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new DeleteEventResponse(printers.Count);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}
