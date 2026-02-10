// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application
#pragma warning disable CA1515, CA1848, CA1031

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// HTTP client implementation for the Print Service backend API.
/// </summary>
public sealed class PrintServiceApiClient : IPrintServiceApiClient
{
    private const string PrinterKeyHeader = "X-Printer-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _configService;
    private readonly ILogger<PrintServiceApiClient> _logger;

    public PrintServiceApiClient(
        HttpClient httpClient,
        IConfigurationService configService,
        ILogger<PrintServiceApiClient> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PrintJobDto>> PollJobsAsync(int maxJobs = 10)
    {
        if (!_configService.IsConfigured)
        {
            _logger.LogWarning("Cannot poll jobs: client not configured");
            return [];
        }

        try
        {
            var baseUrl = _configService.Current.BackendUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/print/v1/printer/jobs?maxJobs={maxJobs}");
            request.Headers.Add(PrinterKeyHeader, _configService.Current.ApiKey);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogDebug("No jobs available");
                return [];
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Authentication failed during poll");
                return [];
            }

            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PollJobsResponse>>(JsonOptions);
            var jobs = apiResponse?.Data?.Jobs ?? [];

            _logger.LogInformation("Retrieved {Count} jobs from backend", jobs.Count);
            return jobs;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to poll for jobs");
            return [];
        }
    }

    public async Task<bool> AcknowledgeJobAsync(string printJobId, bool success, string? failureReason = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        if (!_configService.IsConfigured)
        {
            _logger.LogWarning("Cannot acknowledge job: client not configured");
            return false;
        }

        try
        {
            var requestBody = new
            {
                success,
                failureReason
            };

            var baseUrl = _configService.Current.BackendUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/print/v1/printer/jobs/{printJobId}/acknowledge");
            request.Headers.Add(PrinterKeyHeader, _configService.Current.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to acknowledge job {JobId}: {StatusCode}", printJobId, response.StatusCode);
                return false;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AcknowledgeJobResponse>>(JsonOptions);
            var acknowledged = apiResponse?.Data?.Acknowledged ?? false;

            if (acknowledged)
            {
                _logger.LogInformation("Successfully acknowledged job {JobId} (success: {Success})", printJobId, success);
            }

            return acknowledged;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to acknowledge job {JobId}", printJobId);
            return false;
        }
    }

    public async Task<PrinterInfo?> ValidateConnectionAsync()
    {
        if (!_configService.IsConfigured)
        {
            _logger.LogWarning("Cannot validate connection: client not configured");
            return null;
        }

        try
        {
            // We validate by polling - if we can poll successfully, the connection is valid
            var baseUrl = _configService.Current.BackendUrl.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/print/v1/printer/jobs?maxJobs=1");
            request.Headers.Add(PrinterKeyHeader, _configService.Current.ApiKey);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("API key validation failed");
                return null;
            }

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("Connection validated successfully");
                // Return a placeholder - in a real implementation we might have a dedicated endpoint
                return new PrinterInfo
                {
                    PrinterId = "connected",
                    PrinterName = "Printer",
                    EventName = "Event"
                };
            }

            _logger.LogWarning("Connection validation failed: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to validate connection");
            return null;
        }
    }
}
