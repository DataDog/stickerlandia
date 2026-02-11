/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core.GetPrinters;

namespace Stickerlandia.PrintService.Api;

internal static class GetPrinterStatusesEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        [FromServices] GetPrinterStatusesQueryHandler handler)
    {
        var query = new GetPrinterStatusesQuery
        {
            EventName = eventName
        };

        var response = await handler.Handle(query);

        return Results.Ok(new ApiResponse<GetPrinterStatusesResponse>(response));
    }
}
