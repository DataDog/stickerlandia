// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.PrintService.Core.Observability;

namespace Stickerlandia.PrintService.Core.GetPrinters;

public class GetDistinctEventsQueryHandler(IPrinterRepository repository)
{
    public async Task<List<string>> Handle(GetDistinctEventsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var activity = PrintJobInstrumentation.ActivitySource.StartActivity("Printer.GetDistinctEvents", ActivityKind.Internal);

        try
        {
            var events = await repository.GetDistinctEventNamesAsync();

            activity?.SetTag("print.result_count", events.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return events;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            throw;
        }
    }
}