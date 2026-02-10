// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Globalization;
using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Agnostic;

public class KafkaEventPublisher(ProducerConfig config, ILogger<KafkaEventPublisher> logger) : IPrintServiceEventPublisher
{
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

    private async Task Publish<T>(CloudEvent cloudEvent)
    {
        var activeSpan = Tracer.Instance.ActiveScope?.Span;
        IScope? processScope = null;

        try
        {
            if (activeSpan != null)
            {
                processScope = Tracer.Instance.StartActive($"publish {cloudEvent.Type}", new SpanCreationSettings
                {
                    Parent = activeSpan.Context
                });

                // Convert TraceId and SpanId to proper hex format for W3C traceparent
                var traceIdHex = activeSpan.TraceId.ToString("x32", CultureInfo.InvariantCulture).PadLeft(32, '0');
                var spanIdHex = activeSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture).PadLeft(16, '0');
                cloudEvent.SetAttributeFromString("traceparent", $"00-{traceIdHex}-{spanIdHex}-01");
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
        }
        catch (Exception ex)
        {
            Log.MessagePublishingError(logger, "Error publishing message", ex);
            processScope?.Span.SetException(ex);
            throw;
        }
        finally
        {
            processScope?.Close();
        }
    }
}