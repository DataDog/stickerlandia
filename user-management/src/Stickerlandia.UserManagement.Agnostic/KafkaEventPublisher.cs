// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net.Mime;
using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.Agnostic;

public class KafkaEventPublisher(ProducerConfig config, ILogger<KafkaEventPublisher> logger) : IUserEventPublisher
{
    [Channel("users.userRegistered.v1")]
    [PublishOperation(typeof(UserRegisteredEvent))]
    public async Task PublishUserRegisteredEventV1(UserRegisteredEvent userRegisteredEvent)
    {
        ArgumentNullException.ThrowIfNull(userRegisteredEvent, nameof(userRegisteredEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = userRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = userRegisteredEvent
        };

        await Publish(cloudEvent);
    }

    private async Task Publish(CloudEvent cloudEvent)
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

                cloudEvent.SetAttributeFromString("traceparent", $"00-{activeSpan.TraceId}-{activeSpan.SpanId}[01");
            }

            var formatter = new JsonEventFormatter<UserRegisteredEvent>();
            var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);

            using var producer = new ProducerBuilder<string, string>(config).Build();

            try
            {
                var deliveryReport = await producer.ProduceAsync(cloudEvent.Type,
                    new Message<string, string> { Key = cloudEvent.Id!, Value = Encoding.UTF8.GetString(data.Span) },
                    default);
            
                if (deliveryReport.Status == PersistenceStatus.PossiblyPersisted) {
                    // Handle potential timeout errors
                    throw new MessageProcessingException("Kafka message possibly persisted, but not confirmed.");
                }
                else if (deliveryReport.Status == PersistenceStatus.NotPersisted) {
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