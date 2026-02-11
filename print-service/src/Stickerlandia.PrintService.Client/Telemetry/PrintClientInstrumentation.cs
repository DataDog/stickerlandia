// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Telemetry;

/// <summary>
/// OpenTelemetry instrumentation for the printer client application.
/// Tracks polling cycles, job processing, acknowledgments, and connection health.
/// </summary>
internal sealed class PrintClientInstrumentation : IDisposable
{
    public const string ServiceName = "Stickerlandia.PrintService.Client";
    public const string ServiceVersion = "1.0.0";

    private readonly Meter _meter;

    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    // Poll cycle metrics
    private readonly Counter<long> _pollCyclesCounter;
    private readonly Counter<long> _pollErrorsCounter;
    private readonly Histogram<double> _pollCycleDuration;

    // Job processing metrics
    private readonly Counter<long> _jobsReceivedCounter;
    private readonly Counter<long> _jobsProcessedCounter;
    private readonly Counter<long> _jobsFailedCounter;
    private readonly Histogram<double> _jobProcessingDuration;

    // Acknowledgment metrics
    private readonly Counter<long> _acknowledgementsSucceededCounter;
    private readonly Counter<long> _acknowledgementsFailedCounter;
    private readonly Counter<long> _acknowledgementRetriesCounter;

    // Connection metrics
    private readonly Counter<long> _connectionStatusChangesCounter;

    public PrintClientInstrumentation()
    {
        _meter = new Meter(ServiceName, ServiceVersion);

        _pollCyclesCounter = _meter.CreateCounter<long>(
            "client.poll_cycles_total",
            unit: "{cycle}",
            description: "Total number of poll cycles executed");

        _pollErrorsCounter = _meter.CreateCounter<long>(
            "client.poll_errors_total",
            unit: "{error}",
            description: "Total number of poll cycle errors");

        _pollCycleDuration = _meter.CreateHistogram<double>(
            "client.poll_cycle_duration_seconds",
            unit: "s",
            description: "Duration of poll cycles");

        _jobsReceivedCounter = _meter.CreateCounter<long>(
            "client.jobs_received_total",
            unit: "{job}",
            description: "Total number of jobs received from backend");

        _jobsProcessedCounter = _meter.CreateCounter<long>(
            "client.jobs_processed_total",
            unit: "{job}",
            description: "Total number of jobs processed locally");

        _jobsFailedCounter = _meter.CreateCounter<long>(
            "client.jobs_failed_total",
            unit: "{job}",
            description: "Total number of jobs that failed local processing");

        _jobProcessingDuration = _meter.CreateHistogram<double>(
            "client.job_processing_duration_seconds",
            unit: "s",
            description: "Duration of individual job processing");

        _acknowledgementsSucceededCounter = _meter.CreateCounter<long>(
            "client.acknowledgements_succeeded_total",
            unit: "{ack}",
            description: "Total number of successful job acknowledgements");

        _acknowledgementsFailedCounter = _meter.CreateCounter<long>(
            "client.acknowledgements_failed_total",
            unit: "{ack}",
            description: "Total number of failed job acknowledgements");

        _acknowledgementRetriesCounter = _meter.CreateCounter<long>(
            "client.acknowledgement_retries_total",
            unit: "{retry}",
            description: "Total number of acknowledgement retries for previously unacknowledged jobs");

        _connectionStatusChangesCounter = _meter.CreateCounter<long>(
            "client.connection_status_changes_total",
            unit: "{change}",
            description: "Total number of connection status transitions");
    }

    public void RecordPollCycle(int jobCount, TimeSpan duration)
    {
        _pollCyclesCounter.Add(1);
        _pollCycleDuration.Record(duration.TotalSeconds);
        if (jobCount > 0)
        {
            _jobsReceivedCounter.Add(jobCount);
        }
    }

    public void RecordPollError() => _pollErrorsCounter.Add(1);

    public void RecordJobProcessed(string jobId, TimeSpan duration)
    {
        _jobsProcessedCounter.Add(1);
        _jobProcessingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("status", "success"));
    }

    public void RecordJobFailed(string jobId, TimeSpan duration)
    {
        _jobsFailedCounter.Add(1);
        _jobProcessingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("status", "failed"));
    }

    public void RecordAcknowledgementSucceeded() => _acknowledgementsSucceededCounter.Add(1);
    public void RecordAcknowledgementFailed() => _acknowledgementsFailedCounter.Add(1);
    public void RecordAcknowledgementRetry(int count) => _acknowledgementRetriesCounter.Add(count);

    public void RecordConnectionStatusChange(string newStatus)
    {
        _connectionStatusChangesCounter.Add(1,
            new KeyValuePair<string, object?>("status", newStatus));
    }

    // --- Activity starters ---

    public static Activity? StartPollCycleActivity()
    {
        return ActivitySource.StartActivity("Client.PollCycle", ActivityKind.Consumer);
    }

    public static Activity? StartProcessJobActivity(PrintJobDto job)
    {
        var activity = ActivitySource.StartActivity("Client.ProcessJob", ActivityKind.Internal, parentContext: default);
        activity?.SetTag("print.job_id", job.PrintJobId);
        activity?.SetTag("print.sticker_id", job.StickerId);
        return activity;
    }

    public static Activity? StartAcknowledgeActivity(string jobId, bool success)
    {
        var activity = ActivitySource.StartActivity("Client.Acknowledge", ActivityKind.Producer);
        activity?.SetTag("print.job_id", jobId);
        activity?.SetTag("print.success", success);
        return activity;
    }

    public static Activity? StartRetryAcknowledgementsActivity(int count)
    {
        var activity = ActivitySource.StartActivity("Client.RetryAcknowledgements", ActivityKind.Internal);
        activity?.SetTag("print.retry_count", count);
        return activity;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
