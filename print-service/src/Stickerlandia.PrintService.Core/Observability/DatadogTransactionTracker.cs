// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Stickerlandia.PrintService.Core.Observability;

public class DatadogTransactionTracker : IDatadogTransactionTracker
{
    private readonly string _service;
    private readonly string _environment;
    private readonly string _ddApiKey;
    private readonly Uri _pipelineStatsEndpoint;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatadogTransactionTracker> _logger;

    public DatadogTransactionTracker(IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DatadogTransactionTracker> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _service = configuration["DD_SERVICE"] ?? "print-service";
        _environment = configuration["DD_ENV"] ?? "local";
        _ddApiKey = configuration["DD_API_KEY"] ?? "";
        _pipelineStatsEndpoint = new Uri($"https://trace.agent.{configuration["DD_SITE"] ?? "datadoghq.com"}/api/v0.1/pipeline_stats");
    }

    public async Task TrackTransactionAsync(string transactionId, string checkpoint)
    {
        try
        {
            if (Activity.Current is not null)
            {
                Activity.Current.SetTag("dsm.transaction_id", transactionId);
                Activity.Current.SetTag("dsm.transaction.checkpoint", checkpoint);
            }
            
            var timestampNanos = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000L)
                .ToString(CultureInfo.InvariantCulture);

            var payload = new
            {
                transactions = new[]
                {
                    new
                    {
                        transaction_id = transactionId,
                        checkpoint,
                        timestamp_nanos = timestampNanos
                    }
                },
                service = _service,
                environment = _environment
            };

            var json = JsonSerializer.Serialize(payload);
            var compressed = Gzip(Encoding.UTF8.GetBytes(json));

            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, _pipelineStatsEndpoint);
            request.Content = new ByteArrayContent(compressed);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content.Headers.ContentEncoding.Add("gzip");
            request.Content.Headers.Add("DD-API-KEY", _ddApiKey);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            #pragma warning disable
            _logger.LogInformation("Successfully tracked transaction {TransactionId} at checkpoint {Checkpoint} with status code {StatusCode}", transactionId, checkpoint, response.StatusCode);
            #pragma warning enable
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.TransactionTrackingFailed(_logger, transactionId, checkpoint, ex);
        }
    }

    private static byte[] Gzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(data);
        }

        return output.ToArray();
    }
}
