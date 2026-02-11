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

namespace Stickerlandia.PrintService.Core.GetPrinters;

public class GetPrintersForEventQueryHandler(IPrinterRepository repository, PrintJobInstrumentation instrumentation)
{
    public async Task<List<PrinterDTO>> Handle(GetPrintersForEventQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.EventName is null)
        {
            throw new ArgumentException("Invalid auth token");
        }

        using var activity = PrintJobInstrumentation.StartGetPrintersActivity(query.EventName);

        try
        {
            var printers = await repository.GetPrintersForEventAsync(query.EventName);
            var result = printers.Select(printer => new PrinterDTO(printer)).ToList();

            activity?.SetTag("print.result_count", result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            instrumentation.RecordPrintersQueried(query.EventName, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}