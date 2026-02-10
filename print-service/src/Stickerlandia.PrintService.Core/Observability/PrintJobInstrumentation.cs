// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stickerlandia.PrintService.Core.Observability;

/// <summary>
/// OpenTelemetry instrumentation for print job operations.
/// Provides metrics and tracing for monitoring print service health.
/// </summary>
public sealed class PrintJobInstrumentation : IDisposable
{
    /// <summary>
    /// The name used for the meter and activity source.
    /// </summary>
    public const string ServiceName = "Stickerlandia.PrintService";

    /// <summary>
    /// The version of the instrumentation.
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    private readonly Meter _meter;

    /// <summary>
    /// Activity source for distributed tracing of print job operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    // Counters
    private readonly Counter<long> _jobsSubmittedCounter;
    private readonly Counter<long> _jobsCompletedCounter;
    private readonly Counter<long> _jobsFailedCounter;
    private readonly Counter<long> _jobsPolledCounter;

    // Histograms
    private readonly Histogram<double> _jobProcessingDuration;
    private readonly Histogram<double> _pollLatency;

    // Gauges (using ObservableGauge for queue depth)
    private long _currentQueueDepth;

    /// <summary>
    /// Initializes the print job instrumentation with metrics.
    /// </summary>
    public PrintJobInstrumentation()
    {
        _meter = new Meter(ServiceName, ServiceVersion);

        // Counters
        _jobsSubmittedCounter = _meter.CreateCounter<long>(
            "print_jobs_submitted_total",
            unit: "{job}",
            description: "Total number of print jobs submitted");

        _jobsCompletedCounter = _meter.CreateCounter<long>(
            "print_jobs_completed_total",
            unit: "{job}",
            description: "Total number of print jobs completed successfully");

        _jobsFailedCounter = _meter.CreateCounter<long>(
            "print_jobs_failed_total",
            unit: "{job}",
            description: "Total number of print jobs that failed");

        _jobsPolledCounter = _meter.CreateCounter<long>(
            "print_jobs_polled_total",
            unit: "{job}",
            description: "Total number of print jobs retrieved via polling");

        // Histograms
        _jobProcessingDuration = _meter.CreateHistogram<double>(
            "print_job_processing_duration_seconds",
            unit: "s",
            description: "Time from job submission to completion/failure");

        _pollLatency = _meter.CreateHistogram<double>(
            "print_job_poll_latency_seconds",
            unit: "s",
            description: "Latency of poll requests");

        // Observable gauge for queue depth
        _meter.CreateObservableGauge(
            "print_job_queue_depth",
            () => _currentQueueDepth,
            unit: "{job}",
            description: "Current number of jobs in the queue");
    }

    /// <summary>
    /// Records a job submission.
    /// </summary>
    public void RecordJobSubmitted(string printerId, string eventName)
    {
        _jobsSubmittedCounter.Add(1,
            new KeyValuePair<string, object?>("printer_id", printerId),
            new KeyValuePair<string, object?>("event_name", eventName));
    }

    /// <summary>
    /// Records a job completion.
    /// </summary>
    public void RecordJobCompleted(string printerId, string eventName, TimeSpan processingTime)
    {
        _jobsCompletedCounter.Add(1,
            new KeyValuePair<string, object?>("printer_id", printerId),
            new KeyValuePair<string, object?>("event_name", eventName));

        _jobProcessingDuration.Record(processingTime.TotalSeconds,
            new KeyValuePair<string, object?>("printer_id", printerId),
            new KeyValuePair<string, object?>("event_name", eventName),
            new KeyValuePair<string, object?>("status", "completed"));
    }

    /// <summary>
    /// Records a job failure.
    /// </summary>
    public void RecordJobFailed(string printerId, string eventName, TimeSpan processingTime, string? reason = null)
    {
        _jobsFailedCounter.Add(1,
            new KeyValuePair<string, object?>("printer_id", printerId),
            new KeyValuePair<string, object?>("event_name", eventName),
            new KeyValuePair<string, object?>("failure_reason", reason ?? "unknown"));

        _jobProcessingDuration.Record(processingTime.TotalSeconds,
            new KeyValuePair<string, object?>("printer_id", printerId),
            new KeyValuePair<string, object?>("event_name", eventName),
            new KeyValuePair<string, object?>("status", "failed"));
    }

    /// <summary>
    /// Records jobs polled by a printer.
    /// </summary>
    public void RecordJobsPolled(string printerId, int jobCount, TimeSpan pollDuration)
    {
        _jobsPolledCounter.Add(jobCount,
            new KeyValuePair<string, object?>("printer_id", printerId));

        _pollLatency.Record(pollDuration.TotalSeconds,
            new KeyValuePair<string, object?>("printer_id", printerId));
    }

    /// <summary>
    /// Updates the current queue depth.
    /// </summary>
    public void SetQueueDepth(long depth)
    {
        Interlocked.Exchange(ref _currentQueueDepth, depth);
    }

    /// <summary>
    /// Starts a new activity for job submission.
    /// </summary>
    public static Activity? StartSubmitJobActivity(string eventName, string printerName, string stickerId)
    {
        var activity = ActivitySource.StartActivity("PrintJob.Submit", ActivityKind.Producer);
        activity?.SetTag("print.event_name", eventName);
        activity?.SetTag("print.printer_name", printerName);
        activity?.SetTag("print.sticker_id", stickerId);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for job polling.
    /// </summary>
    public static Activity? StartPollJobsActivity(string printerId)
    {
        var activity = ActivitySource.StartActivity("PrintJob.Poll", ActivityKind.Consumer);
        activity?.SetTag("print.printer_id", printerId);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for job acknowledgment.
    /// </summary>
    public static Activity? StartAcknowledgeJobActivity(string printJobId, bool success)
    {
        var activity = ActivitySource.StartActivity("PrintJob.Acknowledge", ActivityKind.Consumer);
        activity?.SetTag("print.job_id", printJobId);
        activity?.SetTag("print.success", success);
        return activity;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
