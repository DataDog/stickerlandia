/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// AcknowledgePrintJobRequest is instantiated by ASP.NET model binding
#pragma warning disable CA1812

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Api.Configurations;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Api;

internal static class AcknowledgePrintJobEndpoint
{
    public static async Task<IResult> HandleAsync(
        string printJobId,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<AcknowledgePrintJobCommand, AcknowledgePrintJobResponse> handler,
        [FromBody] AcknowledgePrintJobRequest request,
        CancellationToken cancellationToken)
    {
        var printerId = user.FindFirstValue(PrinterKeyAuthenticationHandler.PrinterIdClaimType);

        if (string.IsNullOrEmpty(printerId))
        {
            return Results.Unauthorized();
        }

        var command = new AcknowledgePrintJobCommand
        {
            PrintJobId = printJobId,
            Success = request.Success,
            FailureReason = request.FailureReason,
            PrinterId = printerId
        };

        var response = await handler.Handle(command, cancellationToken);

        return Results.Ok(new ApiResponse<AcknowledgePrintJobResponse>(response));
    }
}

/// <summary>
/// Request body for acknowledging a print job.
/// </summary>
internal sealed record AcknowledgePrintJobRequest
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
}