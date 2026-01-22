/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Api.Helpers;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.GetPrinters;

namespace Stickerlandia.PrintService.Api;

internal static class GetPrintersForEvent
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        HttpContext context,
        [FromServices] GetPrintersForEventQueryHandler handler)
    {
        var result = await handler.Handle(new GetPrintersForEventQuery(eventName));

        return Results.Ok(new ApiResponse<List<PrinterDTO>>(result));
    }
}