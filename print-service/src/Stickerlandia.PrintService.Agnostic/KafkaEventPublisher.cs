// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Diagnostics;
using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Agnostic;

public class KafkaEventPublisher(ProducerConfig config, ILogger<KafkaEventPublisher> logger) : IPrintServiceEventPublisher
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

        await Publish<PrinterRegisteredEvent>(cloudEvent);
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

        await Publish<PrinterRegisteredEvent>(cloudEvent);
    }

    [Channel("print.printerRegistered.v1")]
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

        await Publish<PrinterRegisteredEvent>(cloudEvent);
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

        await Publish<PrintJobCompletedEvent>(cloudEvent);
    }

    [Channel("print.printerDeleted.v1")]
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

        await Publish<PrinterDeletedEvent>(cloudEvent);
    }

    private async Task Publish<T>(CloudEvent cloudEvent)
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

            var formatter = new JsonEventFormatter<T>();
            var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);

            using var producer = new ProducerBuilder<string, string>(config).Build();

            try
            {
                var deliveryReport = await producer.ProduceAsync(cloudEvent.Type,
                    new Message<string, string> { Key = cloudEvent.Id!, Value = Encoding.UTF8.GetString(data.Span) },
                    default);

                if (deliveryReport.Status == PersistenceStatus.PossiblyPersisted)
                {
                    // Handle potential timeout errors
                    throw new MessageProcessingException("Kafka message possibly persisted, but not confirmed.");
                }
                else if (deliveryReport.Status == PersistenceStatus.NotPersisted)
                {
                    // Handle message not persisted errors.
                    throw new MessageProcessingException($"Message not persisted: {deliveryReport.TopicPartitionOffset}");
                }
            }
            catch (ProduceException<string, string> ex)
            {
                switch (ex.Error.Code)
                {
                    case ErrorCode.ClusterAuthorizationFailed:
                        throw new MessageProcessingException("Cluster authorization failed", ex);
                    case ErrorCode.BrokerNotAvailable:
                        throw new MessageProcessingException("Broker not available", ex);
                    default:
                        throw new MessageProcessingException("Error publishing messages", ex);
                }
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Log.MessagePublishingError(logger, "Error publishing message", ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}