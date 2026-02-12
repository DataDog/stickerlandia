// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;

namespace Stickerlandia.PrintService.Api;

internal static class DeleteEventEndpoint
{
    public static async Task<IResult> HandleAsync(
        string eventName,
        [FromQuery] bool force,
        [FromServices] ICommandHandler<DeleteEventCommand, DeleteEventResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteEventCommand(eventName, force);

        var response = await handler.Handle(command, cancellationToken);

        return Results.Ok(new ApiResponse<DeleteEventResponse>(response));
    }
}
