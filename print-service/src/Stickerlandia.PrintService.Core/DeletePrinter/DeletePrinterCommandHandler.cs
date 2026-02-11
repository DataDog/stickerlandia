// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Core.DeletePrinter;

public class DeletePrinterCommandHandler(
    IOutbox outbox,
    IPrinterRepository printerRepository,
    IPrintJobRepository printJobRepository)
{
    public async Task Handle(DeletePrinterCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(DeletePrinterCommand));

        using var activity = PrintJobInstrumentation.StartDeletePrinterActivity(command.EventName, command.PrinterName);

        try
        {
            var printer = await printerRepository.GetPrinterAsync(command.EventName, command.PrinterName);

            if (printer is null)
            {
                throw new PrinterNotFoundException($"Printer '{command.PrinterName}' not found for event '{command.EventName}'.");
            }

            if (!command.Force)
            {
                var hasProcessingJobs = await printJobRepository.HasJobsInStatusAsync(
                    printer.Id!.Value, PrintJobStatus.Processing);

                if (hasProcessingJobs)
                {
                    throw new PrinterHasActiveJobsException(
                        $"Printer '{command.PrinterName}' has jobs currently being processed. Use force=true to delete anyway.");
                }
            }

            await printJobRepository.DeleteJobsForPrinterAsync(printer.Id!.Value);
            await printerRepository.DeleteAsync(command.EventName, command.PrinterName);

            await outbox.StoreEventFor(new PrinterDeletedEvent(printer));

            activity?.SetTag("print.printer_id", printer.Id!.Value);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}
