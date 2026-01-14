/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.PrintService.Core.GetPrinters;

public class GetPrintersForEventQueryHandler(IPrinterRepository repository)
{
    public async Task<List<PrinterDTO>> Handle(GetPrintersForEventQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        try
        {
            if (query.EventName is null)
            {
                throw new ArgumentException("Invalid auth token");
            }

            var printers = await repository.GetPrintersForEventAsync(query.EventName);

            return printers.Select(printer => new PrinterDTO(printer)).ToList();
        }
        catch (InvalidUserException ex)
        {
            Activity.Current?.AddTag("user.notfound", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}