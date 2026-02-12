/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Core.RegisterPrinter;

public class RegisterPrinterCommandHandler(IOutbox outbox, IPrinterRepository repository, PrintJobInstrumentation instrumentation)
    : ICommandHandler<RegisterPrinterCommand, RegisterPrinterResponse>
{
    public async Task<RegisterPrinterResponse> Handle(RegisterPrinterCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(RegisterPrinterCommand));

        using var activity = PrintJobInstrumentation.StartRegisterPrinterActivity(command.EventName, command.PrinterName);

        try
        {
            var existingPrinter = await repository.PrinterExistsAsync(command.EventName, command.PrinterName);

            if (existingPrinter) throw new PrinterExistsException("A printer exists with this name for this event.");

            var printer = Printer.Register(command.EventName, command.PrinterName);
            await repository.AddPrinterAsync(printer);

            await outbox.StoreEventFor(new PrinterRegisteredEvent
            {
                PrinterId = printer.Id!.Value
            });

            activity?.SetTag("print.printer_id", printer.Id!.Value);
            activity?.SetStatus(ActivityStatusCode.Ok);
            instrumentation.RecordPrinterRegistered(command.EventName);

            return new RegisterPrinterResponse(printer);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}