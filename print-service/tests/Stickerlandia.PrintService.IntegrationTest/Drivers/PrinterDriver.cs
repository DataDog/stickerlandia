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
    private readonly WireMockOidcServer? _oidcServer;

    public PrinterDriver(ITestOutputHelper testOutputHelper, HttpClient httpClient,
        CookieContainer cookieContainer, WireMockOidcServer? oidcServer = null)
    {
        _testOutputHelper = testOutputHelper;
        _oidcServer = oidcServer;

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

    public string GetAdminToken(string userId = "test-admin-user")
    {
        if (_oidcServer != null)
        {
            return JwtTokenGenerator.GenerateRsaToken(
                userId,
                ["admin"],
                _oidcServer.KeyProvider,
                _oidcServer.Issuer,
                TestConstants.TestAudience);
        }

        return JwtTokenGenerator.GenerateToken(
            userId,
            ["admin"],
            TestConstants.TestSigningKey,
            TestConstants.TestIssuer,
            TestConstants.TestAudience);
    }

    public string GetUserToken(string userId = "test-user")
    {
        if (_oidcServer != null)
        {
            return JwtTokenGenerator.GenerateRsaToken(
                userId,
                ["user"],
                _oidcServer.KeyProvider,
                _oidcServer.Issuer,
                TestConstants.TestAudience);
        }

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
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<RegisterPrinterResponse>>(responseBody);
            return apiResponse?.Data;
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

    public async Task<(HttpStatusCode StatusCode, SubmitPrintJobResponse? Response)> SubmitPrintJob(
        string authToken,
        string eventName,
        string printerName,
        SubmitPrintJobRequest printJobRequest)
    {
        _testOutputHelper.WriteLine($"Submitting print job to printer: {printerName} for event: {eventName}");

        var requestBody = JsonSerializer.Serialize(printJobRequest);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/print/v1/event/{eventName}/printer/{printerName}/jobs");
        request.Headers.Add("Authorization", $"Bearer {authToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Submit print job response: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<SubmitPrintJobResponse>>(responseBody);
            return (response.StatusCode, apiResponse?.Data);
        }

        _testOutputHelper.WriteLine($"Submit print job failed: {responseBody}");
        return (response.StatusCode, null);
    }

    public async Task<HttpStatusCode> SubmitPrintJobWithoutAuth(
        string eventName,
        string printerName,
        SubmitPrintJobRequest printJobRequest)
    {
        _testOutputHelper.WriteLine($"Submitting print job without auth to printer: {printerName}");

        var requestBody = JsonSerializer.Serialize(printJobRequest);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/print/v1/event/{eventName}/printer/{printerName}/jobs");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        _testOutputHelper.WriteLine($"Submit print job without auth response: {response.StatusCode}");
        return response.StatusCode;
    }

    public async Task<(HttpStatusCode StatusCode, PollPrintJobsResponse? Response)> PollPrintJobs(
        string apiKey,
        int maxJobs = 10)
    {
        _testOutputHelper.WriteLine($"Polling for print jobs with API key");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/print/v1/printer/jobs?maxJobs={maxJobs}");
        request.Headers.Add("X-Printer-Key", apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Poll print jobs response: {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return (response.StatusCode, new PollPrintJobsResponse { Jobs = [] });
        }

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PollPrintJobsResponse>>(responseBody);
            return (response.StatusCode, apiResponse?.Data);
        }

        _testOutputHelper.WriteLine($"Poll print jobs failed: {responseBody}");
        return (response.StatusCode, null);
    }

    public async Task<HttpStatusCode> PollPrintJobsWithoutAuth()
    {
        _testOutputHelper.WriteLine("Polling for print jobs without API key");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/print/v1/printer/jobs");

        var response = await _httpClient.SendAsync(request);

        _testOutputHelper.WriteLine($"Poll print jobs without auth response: {response.StatusCode}");
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> PollPrintJobsWithInvalidApiKey()
    {
        _testOutputHelper.WriteLine("Polling for print jobs with invalid API key");

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/print/v1/printer/jobs");
        request.Headers.Add("X-Printer-Key", "invalid-api-key-12345");

        var response = await _httpClient.SendAsync(request);

        _testOutputHelper.WriteLine($"Poll print jobs with invalid key response: {response.StatusCode}");
        return response.StatusCode;
    }

    public async Task<(HttpStatusCode StatusCode, AcknowledgePrintJobResponse? Response)> AcknowledgePrintJob(
        string apiKey,
        string printJobId,
        bool success,
        string? failureReason = null)
    {
        _testOutputHelper.WriteLine($"Acknowledging print job: {printJobId} with success: {success}");

        var requestBody = JsonSerializer.Serialize(new AcknowledgePrintJobRequest
        {
            Success = success,
            FailureReason = failureReason
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/print/v1/printer/jobs/{printJobId}/acknowledge");
        request.Headers.Add("X-Printer-Key", apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Acknowledge print job response: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<AcknowledgePrintJobResponse>>(responseBody);
            return (response.StatusCode, apiResponse?.Data);
        }

        _testOutputHelper.WriteLine($"Acknowledge print job failed: {responseBody}");
        return (response.StatusCode, null);
    }

    public async Task<HttpStatusCode> AcknowledgePrintJobWithoutAuth(string printJobId, bool success)
    {
        _testOutputHelper.WriteLine($"Acknowledging print job without auth: {printJobId}");

        var requestBody = JsonSerializer.Serialize(new AcknowledgePrintJobRequest
        {
            Success = success
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/print/v1/printer/jobs/{printJobId}/acknowledge");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        _testOutputHelper.WriteLine($"Acknowledge print job without auth response: {response.StatusCode}");
        return response.StatusCode;
    }

    public async Task<(HttpStatusCode StatusCode, GetPrinterStatusesResponse? Response)> GetPrinterStatuses(
        string authToken,
        string eventName)
    {
        _testOutputHelper.WriteLine($"Getting printer statuses for event: {eventName}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/print/v1/event/{eventName}/printers/status");
        request.Headers.Add("Authorization", $"Bearer {authToken}");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine($"Get printer statuses response: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<GetPrinterStatusesResponse>>(responseBody);
            return (response.StatusCode, apiResponse?.Data);
        }

        _testOutputHelper.WriteLine($"Get printer statuses failed: {responseBody}");
        return (response.StatusCode, null);
    }

    public async Task<HttpStatusCode> GetPrinterStatusesWithoutAuth(string eventName)
    {
        _testOutputHelper.WriteLine($"Getting printer statuses without auth for event: {eventName}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/print/v1/event/{eventName}/printers/status");

        var response = await _httpClient.SendAsync(request);

        _testOutputHelper.WriteLine($"Get printer statuses without auth response: {response.StatusCode}");
        return response.StatusCode;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
