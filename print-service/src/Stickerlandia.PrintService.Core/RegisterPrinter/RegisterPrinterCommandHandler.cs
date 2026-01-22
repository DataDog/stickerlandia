/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.Core.RegisterPrinter;

public class RegisterPrinterCommandHandler(IOutbox outbox, IPrinterRepository repository)
{
    public async Task<RegisterPrinterResponse> Handle(RegisterPrinterCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(RegisterPrinterCommand));

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

            return new RegisterPrinterResponse(printer);
        }
        catch (Exception ex)
        {
            Activity.Current?.AddTag("user.registration.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}