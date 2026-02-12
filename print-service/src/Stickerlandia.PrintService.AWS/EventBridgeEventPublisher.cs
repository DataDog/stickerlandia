/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.Attributes;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;
using Log = Stickerlandia.PrintService.Core.Observability.Log;

namespace Stickerlandia.PrintService.AWS;

[AsyncApi]
public class EventBridgeEventPublisher(
    ILogger<EventBridgeEventPublisher> logger,
    IAmazonEventBridge client,
    IOptions<AwsConfiguration> awsConfiguration) : IPrintServiceEventPublisher
{
    [Channel("printJobs.queued.v1")]
    [PublishOperation(typeof(PrintJobQueuedEvent))]
    public async Task PublishPrintJobQueuedEvent(PrintJobQueuedEvent printJobQueuedEvent)
    {
        ArgumentNullException.ThrowIfNull(printJobQueuedEvent, nameof(printJobQueuedEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printJobQueuedEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printJobQueuedEvent
        };

        await PublishCloudEventAsync(cloudEvent);
    }

    [Channel("printJobs.failed.v1")]
    [PublishOperation(typeof(PrintJobFailedEvent))]
    public async Task PublishPrintJobFailedEvent(PrintJobFailedEvent printJobFailedEvent)
    {
        ArgumentNullException.ThrowIfNull(printJobFailedEvent, nameof(printJobFailedEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printJobFailedEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printJobFailedEvent
        };

        await PublishCloudEventAsync(cloudEvent);
    }
    
    [Channel("printers.registered.v1")]
    [PublishOperation(typeof(PrinterRegisteredEvent))]
    public async Task PublishPrinterRegisteredEvent(PrinterRegisteredEvent printerRegisteredEvent)
    {
        ArgumentNullException.ThrowIfNull(printerRegisteredEvent, nameof(printerRegisteredEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printerRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printerRegisteredEvent
        };

        await PublishCloudEventAsync(cloudEvent);
    }

    [Channel("printJobs.completed.v1")]
    [PublishOperation(typeof(PrintJobCompletedEvent))]
    public async Task PublishPrintJobCompletedEvent(PrintJobCompletedEvent printJobCompletedEvent)
    {
        ArgumentNullException.ThrowIfNull(printJobCompletedEvent, nameof(printJobCompletedEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printJobCompletedEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printJobCompletedEvent
        };

        await PublishCloudEventAsync(cloudEvent);
    }

    [Channel("printers.deleted.v1")]
    [PublishOperation(typeof(PrinterDeletedEvent))]
    public async Task PublishPrinterDeletedEvent(PrinterDeletedEvent printerDeletedEvent)
    {
        ArgumentNullException.ThrowIfNull(printerDeletedEvent, nameof(printerDeletedEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printerDeletedEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printerDeletedEvent
        };

        await PublishCloudEventAsync(cloudEvent);
    }

    internal async Task PublishCloudEventAsync(CloudEvent cloudEvent)
    {
        using var activity = PrintJobInstrumentation.ActivitySource.StartActivity(
            $"publish {cloudEvent.Type}", ActivityKind.Producer);

        try
        {
            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                cloudEvent.SetAttributeFromString("traceparent",
                    $"00-{currentActivity.TraceId}-{currentActivity.SpanId}-01");
            }

            var formatter = new JsonEventFormatter();
            var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);
            var jsonString = System.Text.Encoding.UTF8.GetString(data.Span);

            var response = await client.PutEventsAsync(new PutEventsRequest()
            {
                Entries = new List<PutEventsRequestEntry>(1)
                {
                    new()
                    {
                        EventBusName = awsConfiguration.Value.EventBusName,
                        Source = $"{Environment.GetEnvironmentVariable("ENV") ?? "dev"}.users",
                        DetailType = cloudEvent.Type,
                        Detail = jsonString,
                    }
                }
            });

            if (response.FailedEntryCount is > 0)
            {
                var failedEntries = response.Entries
                    .Where(e => !string.IsNullOrEmpty(e.ErrorCode))
                    .ToList();
                throw new EventBridgePartialFailureException(response.FailedEntryCount.Value, failedEntries);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Log.MessagePublishingError(logger, "Failure publishing event", ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}