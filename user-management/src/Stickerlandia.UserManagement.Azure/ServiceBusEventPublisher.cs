// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Stickerlandia.UserManagement.Core;

namespace Stickerlandia.UserManagement.Azure;

public class ServiceBusEventPublisher(ServiceBusClient client) : IUserEventPublisher
{
    public async Task PublishUserRegisteredEventV1(UserRegisteredEvent userRegisteredEvent)
    {
        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = userRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = userRegisteredEvent
        };
        
        await this.Publish(cloudEvent);
    }

    private async Task Publish(CloudEvent cloudEvent)
    {
        var sender = client.CreateSender(cloudEvent.Type);
        
        var formatter = new JsonEventFormatter<UserRegisteredEvent>();
        var data = formatter.EncodeBinaryModeEventData(cloudEvent);
        
        var serviceBusMessage = new ServiceBusMessage(data)
        {
            ContentType = "application/json"
        };
            
        await sender.SendMessageAsync(serviceBusMessage);
    }
}