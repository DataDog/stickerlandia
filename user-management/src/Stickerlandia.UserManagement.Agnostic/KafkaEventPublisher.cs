// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.Agnostic;

public class KafkaEventPublisher(ProducerConfig config, ILogger<KafkaEventPublisher> logger) : IUserEventPublisher
{
    [Channel("users.userRegistered.v1")]
    [PublishOperation(typeof(UserRegisteredEvent))]
    public async Task PublishUserRegisteredEventV1(UserRegisteredEvent userRegisteredEvent)
    {
        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = userRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = userRegisteredEvent,
        };
        
        await this.Publish(cloudEvent);
    }

    private Task Publish(CloudEvent cloudEvent)
    {
        var activeSpan = Tracer.Instance.ActiveScope?.Span;
        IScope? processScope = null;

        try
        {
            if (activeSpan != null)
            {
                processScope = Tracer.Instance.StartActive($"publish {cloudEvent.Type}", new SpanCreationSettings()
                {
                    Parent = activeSpan.Context
                });

                cloudEvent.SetAttributeFromString("traceparent", $"00-{activeSpan.TraceId}-{activeSpan.SpanId}[01");
            }
            
            var formatter = new JsonEventFormatter<UserRegisteredEvent>();
            var data = formatter.EncodeBinaryModeEventData(cloudEvent);

            using var producer = new ProducerBuilder<string, string>(config).Build();
            
            producer.Produce(cloudEvent.Type, new Message<string, string> { Key = cloudEvent.Id!, Value = Encoding.UTF8.GetString(data.Span) },
                (deliveryReport) =>
                {
                    if (deliveryReport.Error.Code != ErrorCode.NoError) {
                        logger.LogError($"Failed to deliver message: {deliveryReport.Error.Reason}");
                    }
                    else {
                        logger.LogInformation($"Produced event to topic {cloudEvent.Type}");
                    }
                });
                
            producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failure publishing message");
            processScope?.Span.SetException(ex);
        }
        finally
        {
            processScope?.Close();
        }

        return Task.CompletedTask;
    }
}