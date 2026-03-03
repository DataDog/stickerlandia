// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Stickerlandia.PrintService.Core.Observability;

public class DatadogTransactionTracker(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<DatadogTransactionTracker> logger)
{
    private static readonly Uri PipelineStatsEndpoint =
        new("https://trace.agent.datadoghq.com/api/v0.1/pipeline_stats");

    private readonly string _apiKey = configuration["DD_API_KEY"] ?? string.Empty;
    private readonly string _service = configuration["DD_SERVICE"] ?? "print-service";
    private readonly string _environment = configuration["DD_ENV"] ?? "local";

    public async Task TrackTransactionAsync(string transactionId, string checkpoint)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Log.TransactionTrackingSkipped(logger);
            return;
        }

        try
        {
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

            using var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, PipelineStatsEndpoint);
            request.Headers.Add("DD-API-KEY", _apiKey);
            request.Content = new ByteArrayContent(compressed);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content.Headers.ContentEncoding.Add("gzip");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            #pragma warning disable
            logger.LogInformation("Successfully tracked transaction {TransactionId} at checkpoint {Checkpoint} with status code {StatusCode}", transactionId, checkpoint, response.StatusCode);
            #pragma warning enable
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.TransactionTrackingFailed(logger, transactionId, checkpoint, ex);
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
