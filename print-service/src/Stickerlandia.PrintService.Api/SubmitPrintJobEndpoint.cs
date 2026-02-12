/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Api;

internal static class SubmitPrintJobEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        string printerName,
        [FromServices] ICommandHandler<SubmitPrintJobCommand, SubmitPrintJobResponse> handler,
        [FromBody] SubmitPrintJobCommand request)
    {
        request.EventName = eventName;
        request.PrinterName = printerName;

        var response = await handler.Handle(request);

        return Results.Created($"/api/print/v1/printer/jobs/{response.PrintJobId}", new ApiResponse<SubmitPrintJobResponse>(response));
    }
}
