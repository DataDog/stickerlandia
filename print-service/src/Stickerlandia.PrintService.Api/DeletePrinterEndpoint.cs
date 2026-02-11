// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core.DeletePrinter;

namespace Stickerlandia.PrintService.Api;

internal static class DeletePrinterEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        string printerName,
        [FromQuery] bool force,
        [FromServices] DeletePrinterCommandHandler handler)
    {
        var command = new DeletePrinterCommand(eventName, printerName, force);

        await handler.Handle(command);

        return Results.NoContent();
    }
}
