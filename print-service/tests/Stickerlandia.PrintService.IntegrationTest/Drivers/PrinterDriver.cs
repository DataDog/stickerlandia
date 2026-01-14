/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Net;
using System.Text;
using System.Text.Json;
using Stickerlandia.PrintService.IntegrationTest.ViewModels;
using Xunit.Abstractions;

#pragma warning disable CA2234, CA2000

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

internal sealed class PrinterDriver : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public PrinterDriver(ITestOutputHelper testOutputHelper, HttpClient httpClient,
        CookieContainer cookieContainer)
    {
        _testOutputHelper = testOutputHelper;

        var httpHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            CheckCertificateRevocationList = true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(httpHandler, true)
        {
            BaseAddress = httpClient.BaseAddress
        };
    }

    public static string GetAdminToken(string userId = "test-admin-user")
    {
        return JwtTokenGenerator.GenerateToken(
            userId,
            ["admin"],
            TestConstants.TestSigningKey,
            TestConstants.TestIssuer,
            TestConstants.TestAudience);
    }

    public static string GetUserToken(string userId = "test-user")
    {
        return JwtTokenGenerator.GenerateToken(
            userId,
            ["user"],
            TestConstants.TestSigningKey,
            TestConstants.TestIssuer,
            TestConstants.TestAudience);
    }

    public async Task<RegisterPrinterResponse?> RegisterPrinter(string authToken, string eventName, string printerName)
    {
        _testOutputHelper.WriteLine($"Registering printer: {printerName} for event: {eventName}");

        var requestBody = JsonSerializer.Serialize(new RegisterPrinterRequest
        {
            EventName = eventName,
            PrinterName = printerName
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/print/v1/event/{eventName}");
        request.Headers.Add("Authorization", $"Bearer {authToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Register printer response: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            return new RegisterPrinterResponse { Success = true };
        }

        _testOutputHelper.WriteLine($"Register printer failed: {responseBody}");
        return null;
    }

    public async Task<List<PrinterDTO>?> GetPrintersForEvent(string authToken, string eventName)
    {
        _testOutputHelper.WriteLine($"Getting printers for event: {eventName}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/print/v1/event/{eventName}");
        request.Headers.Add("Authorization", $"Bearer {authToken}");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Get printers response: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PrinterDTO>>>(responseBody);
            return apiResponse?.Data;
        }

        _testOutputHelper.WriteLine($"Get printers failed: {responseBody}");
        return null;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
