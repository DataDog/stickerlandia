/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// This class is instantiated via DI by the authentication framework
#pragma warning disable CA1812

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.Api.Configurations;

/// <summary>
/// Authentication handler for printer API key authentication.
/// </summary>
internal sealed class PrinterKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IPrinterKeyValidator printerKeyValidator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PrinterKey";
    public const string HeaderName = "X-Printer-Key";

    public const string PrinterIdClaimType = "PrinterId";
    public const string EventNameClaimType = "EventName";
    public const string PrinterNameClaimType = "PrinterName";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.Fail("API key is empty");
        }

        var printer = await printerKeyValidator.ValidateKeyAsync(apiKey);

        if (printer is null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new[]
        {
            new Claim(PrinterIdClaimType, printer.Id!.Value),
            new Claim(EventNameClaimType, printer.EventName),
            new Claim(PrinterNameClaimType, printer.PrinterName),
            new Claim(ClaimTypes.AuthenticationMethod, SchemeName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
