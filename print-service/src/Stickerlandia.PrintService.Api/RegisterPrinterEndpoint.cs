/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Api.Helpers;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Api;

internal static class RegisterPrinterEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        HttpContext context,
        [FromServices] RegisterPrinterCommandHandler updateHandler,
        [FromBody] RegisterPrinterCommand request)
    {
        await updateHandler.Handle(request);
        
        return Results.Ok(new ApiResponse<string>("OK"));
    }
}