/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Api;

internal static class SubmitPrintJobEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        string printerName,
        [FromServices] SubmitPrintJobCommandHandler handler,
        [FromBody] SubmitPrintJobCommand request)
    {
        var response = await handler.Handle(eventName, printerName, request);

        return Results.Created($"/api/print/v1/printer/jobs/{response.PrintJobId}", new ApiResponse<SubmitPrintJobResponse>(response));
    }
}
