// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class AzureServiceBusMessaging(string connectionString) : IMessaging, IAsyncDisposable
{
    private readonly ServiceBusClient _client = new(connectionString);

    public async Task SendMessageAsync(string queueName, object message)
    {
        var sender = _client.CreateSender(queueName);
        var messageBody = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
        {
            ContentType = "application/json"
        };
            
        await sender.SendMessageAsync(serviceBusMessage);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}