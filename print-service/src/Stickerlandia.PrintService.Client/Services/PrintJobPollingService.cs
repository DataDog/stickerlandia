// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application

#pragma warning disable CA1515, CA1848, CA1031

using System.Diagnostics;
using Datadog.Trace;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Models;
using Stickerlandia.PrintService.Client.Telemetry;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// Background service that polls for print jobs and processes them.
/// </summary>
internal sealed class PrintJobPollingService : BackgroundService
{
    private readonly IPrintServiceApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfigurationService _configService;
    private readonly ClientStatusService _statusService;
    private readonly PrintClientInstrumentation _instrumentation;
    private readonly ILogger<PrintJobPollingService> _logger;

    public PrintJobPollingService(
        IPrintServiceApiClient apiClient,
        ILocalStorageService localStorage,
        IConfigurationService configService,
        ClientStatusService statusService,
        PrintClientInstrumentation instrumentation,
        ILogger<PrintJobPollingService> logger)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _configService = configService;
        _statusService = statusService;
        _instrumentation = instrumentation;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Print job polling service started");

        // Load configuration on startup
        await _configService.LoadAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_configService.IsConfigured)
            {
                _statusService.UpdateConnectionStatus(ConnectionStatus.NotConfigured);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                await ProcessPollCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during poll cycle");
                _statusService.UpdateConnectionStatus(ConnectionStatus.Disconnected, ex.Message);
                _instrumentation.RecordPollError();
                _instrumentation.RecordConnectionStatusChange("Disconnected");
            }

            var interval = TimeSpan.FromSeconds(_configService.Current.PollingIntervalSeconds);
            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("Print job polling service stopped");
    }

    private async Task ProcessPollCycleAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting poll cycle");

        using var activity = PrintClientInstrumentation.StartPollCycleActivity();
        var stopwatch = Stopwatch.StartNew();

        // First, retry any unacknowledged jobs
        await RetryUnacknowledgedJobsAsync();

        // Poll for new jobs
        var jobs = await _apiClient.PollJobsAsync(_configService.Current.MaxJobsPerPoll);

        if (jobs.Count == 0)
        {
            _statusService.UpdateConnectionStatus(ConnectionStatus.Connected);
            _statusService.UpdateLastPoll(DateTimeOffset.UtcNow, 0);

            stopwatch.Stop();
            activity?.SetTag("print.jobs_received", 0);
            activity?.SetStatus(ActivityStatusCode.Ok);
            _instrumentation.RecordPollCycle(0, stopwatch.Elapsed);
            return;
        }

        _logger.LogInformation("Received {Count} jobs to process", jobs.Count);
        _statusService.UpdateConnectionStatus(ConnectionStatus.Connected);
        activity?.SetTag("print.jobs_received", jobs.Count);

        // Process each job
        foreach (var job in jobs)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await ProcessJobAsync(job);
        }

        _statusService.UpdateLastPoll(DateTimeOffset.UtcNow, jobs.Count);
        await UpdateJobCountsAsync();

        stopwatch.Stop();
        activity?.SetStatus(ActivityStatusCode.Ok);
        _instrumentation.RecordPollCycle(jobs.Count, stopwatch.Elapsed);
    }

    private async Task ProcessJobAsync(PrintJobDto job)
    {
        _logger.LogInformation("Processing job {JobId} for sticker {StickerId}", job.PrintJobId, job.StickerId);

        using var activity = PrintClientInstrumentation.StartProcessJobActivity(job);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Store job locally
            await _localStorage.StoreJobAsync(job);

            // In a real implementation, this is where we would:
            // 1. Download the sticker image from job.StickerUrl
            // 2. Send it to the local printer
            // 3. Wait for print confirmation

            // For now, we simulate instant success
            await _localStorage.MarkCompletedAsync(job.PrintJobId);

            stopwatch.Stop();
            _instrumentation.RecordJobProcessed(job.PrintJobId, stopwatch.Elapsed);

            // Acknowledge to backend
            using var ackActivity = PrintClientInstrumentation.StartAcknowledgeActivity(job.PrintJobId, true);
            var acknowledged = await _apiClient.AcknowledgeJobAsync(job.PrintJobId, true);

            if (acknowledged)
            {
                await _localStorage.MarkAcknowledgedAsync(job.PrintJobId);
                _logger.LogInformation("Job {JobId} processed and acknowledged", job.PrintJobId);
                ackActivity?.SetStatus(ActivityStatusCode.Ok);
                _instrumentation.RecordAcknowledgementSucceeded();

                // Mark consumption as complete
                var propagator = new SpanContextExtractor();
                var extractedContext = propagator.ExtractIncludingDsm(
                    job,
                    GetHeader,
                    "queue",
                    "print_queue");
                using var scope = Tracer.Instance.StartActive(
                    "processed print.job",
                    new SpanCreationSettings
                    {
                        Parent = extractedContext
                    });
            }
            else
            {
                _logger.LogWarning("Job {JobId} processed but acknowledgment failed, will retry", job.PrintJobId);
                ackActivity?.SetStatus(ActivityStatusCode.Error, "Acknowledgement not confirmed by backend");
                _instrumentation.RecordAcknowledgementFailed();
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to process job {JobId}", job.PrintJobId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            _instrumentation.RecordJobFailed(job.PrintJobId, stopwatch.Elapsed);

            // Mark as failed locally
            await _localStorage.MarkFailedAsync(job.PrintJobId, ex.Message);

            // Try to acknowledge failure to backend
            var acknowledged = await _apiClient.AcknowledgeJobAsync(job.PrintJobId, false, ex.Message);
            if (acknowledged)
            {
                await _localStorage.MarkAcknowledgedAsync(job.PrintJobId);
                _instrumentation.RecordAcknowledgementSucceeded();
            }
            else
            {
                _instrumentation.RecordAcknowledgementFailed();
            }
        }
    }

    private async Task RetryUnacknowledgedJobsAsync()
    {
        var unacknowledged = await _localStorage.GetUnacknowledgedJobsAsync();

        if (unacknowledged.Count == 0) return;

        _logger.LogInformation("Retrying {Count} unacknowledged jobs", unacknowledged.Count);

        using var activity = PrintClientInstrumentation.StartRetryAcknowledgementsActivity(unacknowledged.Count);
        _instrumentation.RecordAcknowledgementRetry(unacknowledged.Count);

        foreach (var job in unacknowledged)
        {
            var success = job.Status == "Completed";
            var acknowledged = await _apiClient.AcknowledgeJobAsync(
                job.PrintJobId,
                success,
                job.FailureReason);

            if (acknowledged)
            {
                await _localStorage.MarkAcknowledgedAsync(job.PrintJobId);
                _logger.LogInformation("Successfully acknowledged previously failed job {JobId}", job.PrintJobId);
                _instrumentation.RecordAcknowledgementSucceeded();
            }
            else
            {
                _instrumentation.RecordAcknowledgementFailed();
            }
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private async Task UpdateJobCountsAsync()
    {
        var today = await _localStorage.GetJobsProcessedTodayAsync();
        var total = await _localStorage.GetTotalJobsProcessedAsync();
        _statusService.UpdateJobCounts(today, total);
    }

    private static IEnumerable<string?> GetHeader(PrintJobDto doc, string key)
    {
        if (doc.PropagationHeaders.TryGetValue(key, out var value)) yield return value;
    }
}