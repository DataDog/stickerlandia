// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Azure;

[AsyncApi]
public class ServiceBusEventPublisher(ILogger<ServiceBusEventPublisher> logger, ServiceBusClient client)
    : IUserEventPublisher
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

            var sender = client.CreateSender(cloudEvent.Type);

            var formatter = new JsonEventFormatter<UserRegisteredEvent>();
            var data = formatter.EncodeBinaryModeEventData(cloudEvent);

            var serviceBusMessage = new ServiceBusMessage(data)
            {
                ContentType = "application/json"
            };

            await sender.SendMessageAsync(serviceBusMessage);
        }
        catch (Exception ex)
        {
            Log.MessagePublishingError(logger, "Error publishing event", ex);
            processScope?.Span.SetException(ex);
            throw;
        }
        finally
        {
            processScope?.Close();
        }
    }
}