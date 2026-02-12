/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Api;

internal static class RegisterPrinterEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        HttpContext context,
        [FromServices] ICommandHandler<RegisterPrinterCommand, RegisterPrinterResponse> handler,
        [FromBody] RegisterPrinterCommand request,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(request, cancellationToken);

        return Results.Created($"/api/print/v1/event/{eventName}", new ApiResponse<RegisterPrinterResponse>(response));
    }
}