// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Stickerlandia.PrintService.Core.Observability;

/// <summary>
/// OpenTelemetry instrumentation for print service operations.
/// Provides metrics and tracing for monitoring print service health and business activity.
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
    /// Activity source for distributed tracing of print service operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    // Print job counters
    private readonly Counter<long> _jobsSubmittedCounter;
    private readonly Counter<long> _jobsCompletedCounter;
    private readonly Counter<long> _jobsFailedCounter;
    private readonly Counter<long> _jobsPolledCounter;

    // Printer counters
    private readonly Counter<long> _printersRegisteredCounter;
    private readonly Counter<long> _printersQueriedCounter;
    private readonly Counter<long> _printerStatusChecksCounter;

    // Outbox counters
    private readonly Counter<long> _outboxItemsProcessedCounter;
    private readonly Counter<long> _outboxItemsFailedCounter;

    // Print job histograms
    private readonly Histogram<double> _jobProcessingDuration;
    private readonly Histogram<double> _pollLatency;

    // Outbox histograms
    private readonly Histogram<double> _outboxProcessingDuration;

    // Gauges (using ObservableGauge for queue depth)
    private long _currentQueueDepth;

    /// <summary>
    /// Initializes the print service instrumentation with metrics.
    /// </summary>
    public PrintJobInstrumentation()
    {
        _meter = new Meter(ServiceName, ServiceVersion);

        // Print job counters
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

        // Printer counters
        _printersRegisteredCounter = _meter.CreateCounter<long>(
            "printers_registered_total",
            unit: "{printer}",
            description: "Total number of printers registered");

        _printersQueriedCounter = _meter.CreateCounter<long>(
            "printers_queried_total",
            unit: "{query}",
            description: "Total number of printer list queries");

        _printerStatusChecksCounter = _meter.CreateCounter<long>(
            "printer_status_checks_total",
            unit: "{check}",
            description: "Total number of printer status check queries");

        // Outbox counters
        _outboxItemsProcessedCounter = _meter.CreateCounter<long>(
            "outbox_items_processed_total",
            unit: "{item}",
            description: "Total number of outbox items successfully processed");

        _outboxItemsFailedCounter = _meter.CreateCounter<long>(
            "outbox_items_failed_total",
            unit: "{item}",
            description: "Total number of outbox items that failed processing");

        // Print job histograms
        _jobProcessingDuration = _meter.CreateHistogram<double>(
            "print_job_processing_duration_seconds",
            unit: "s",
            description: "Time from job submission to completion/failure");

        _pollLatency = _meter.CreateHistogram<double>(
            "print_job_poll_latency_seconds",
            unit: "s",
            description: "Latency of poll requests");

        // Outbox histograms
        _outboxProcessingDuration = _meter.CreateHistogram<double>(
            "outbox_processing_duration_seconds",
            unit: "s",
            description: "Duration of outbox processing cycles");

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
    /// Records a printer registration.
    /// </summary>
    public void RecordPrinterRegistered(string eventName)
    {
        _printersRegisteredCounter.Add(1,
            new KeyValuePair<string, object?>("event_name", eventName));
    }

    /// <summary>
    /// Records a printer list query.
    /// </summary>
    public void RecordPrintersQueried(string eventName, int count)
    {
        _printersQueriedCounter.Add(1,
            new KeyValuePair<string, object?>("event_name", eventName),
            new KeyValuePair<string, object?>("result_count", count));
    }

    /// <summary>
    /// Records a printer status check.
    /// </summary>
    public void RecordPrinterStatusCheck(string eventName, int onlineCount, int offlineCount)
    {
        _printerStatusChecksCounter.Add(1,
            new KeyValuePair<string, object?>("event_name", eventName),
            new KeyValuePair<string, object?>("online_count", onlineCount),
            new KeyValuePair<string, object?>("offline_count", offlineCount));
    }

    /// <summary>
    /// Records a successful outbox item processing.
    /// </summary>
    public void RecordOutboxItemProcessed(string eventType)
    {
        _outboxItemsProcessedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records a failed outbox item processing.
    /// </summary>
    public void RecordOutboxItemFailed(string eventType, string reason)
    {
        _outboxItemsFailedCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("failure_reason", reason));
    }

    /// <summary>
    /// Records the duration of an outbox processing cycle.
    /// </summary>
    public void RecordOutboxProcessingDuration(TimeSpan duration, int itemCount)
    {
        _outboxProcessingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("items_count", itemCount));
    }

    // --- Activity starters ---

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

    /// <summary>
    /// Starts a new activity for printer registration.
    /// </summary>
    public static Activity? StartRegisterPrinterActivity(string eventName, string printerName)
    {
        var activity = ActivitySource.StartActivity("Printer.Register", ActivityKind.Internal);
        activity?.SetTag("print.event_name", eventName);
        activity?.SetTag("print.printer_name", printerName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for querying printers for an event.
    /// </summary>
    public static Activity? StartGetPrintersActivity(string eventName)
    {
        var activity = ActivitySource.StartActivity("Printer.GetForEvent", ActivityKind.Internal);
        activity?.SetTag("print.event_name", eventName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for checking printer statuses.
    /// </summary>
    public static Activity? StartGetPrinterStatusesActivity(string eventName)
    {
        var activity = ActivitySource.StartActivity("Printer.GetStatuses", ActivityKind.Internal);
        activity?.SetTag("print.event_name", eventName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for deleting a printer.
    /// </summary>
    public static Activity? StartDeletePrinterActivity(string eventName, string printerName)
    {
        var activity = ActivitySource.StartActivity("Printer.Delete", ActivityKind.Internal);
        activity?.SetTag("print.event_name", eventName);
        activity?.SetTag("print.printer_name", printerName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for deleting an event and all its printers.
    /// </summary>
    public static Activity? StartDeleteEventActivity(string eventName)
    {
        var activity = ActivitySource.StartActivity("Event.Delete", ActivityKind.Internal);
        activity?.SetTag("print.event_name", eventName);
        return activity;
    }

    /// <summary>
    /// Starts a new activity for outbox processing cycle.
    /// </summary>
    public static Activity? StartOutboxProcessingActivity()
    {
        return ActivitySource.StartActivity("Outbox.Process", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a new activity for processing a single outbox item.
    /// </summary>
    public static Activity? StartOutboxItemActivity(string eventType, string itemId)
    {
        var activity = ActivitySource.StartActivity("Outbox.ProcessItem", ActivityKind.Producer);
        activity?.SetTag("outbox.event_type", eventType);
        activity?.SetTag("outbox.item_id", itemId);
        return activity;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
