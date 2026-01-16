/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Stickerlandia.PrintService.Api.Configurations;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.Api;

internal static class PollPrintJobsEndpoint
{
    public static async Task<IResult> HandleAsync(
        ClaimsPrincipal user,
        [FromServices] GetPrintJobsForPrinterQueryHandler handler,
        [FromQuery] int maxJobs = 10)
    {
        var printerId = user.FindFirstValue(PrinterKeyAuthenticationHandler.PrinterIdClaimType);

        if (string.IsNullOrEmpty(printerId))
        {
            return Results.Unauthorized();
        }

        var query = new GetPrintJobsForPrinterQuery
        {
            PrinterId = printerId,
            MaxJobs = Math.Clamp(maxJobs, 1, 50)
        };

        var response = await handler.Handle(query);

        if (response.Jobs.Count == 0)
        {
            return Results.NoContent();
        }

        return Results.Ok(new ApiResponse<PollPrintJobsResponse>(new PollPrintJobsResponse
        {
            Jobs = response.Jobs
        }));
    }
}

/// <summary>
/// Response for the poll print jobs endpoint.
/// </summary>
internal sealed record PollPrintJobsResponse
{
    public IReadOnlyList<PrintJobDto> Jobs { get; init; } = [];
}
