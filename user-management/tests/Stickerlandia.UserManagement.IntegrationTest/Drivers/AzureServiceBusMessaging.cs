/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
using Azure.Messaging.ServiceBus;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class AzureServiceBusMessaging(string connectionString) : IMessaging, IAsyncDisposable
{
    private readonly ServiceBusClient _client = new(connectionString);

    public async Task SendMessageAsync(string queueName, string messageJson)
    {
        var sender = _client.CreateSender(queueName);

        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageJson))
        {
            ContentType = "application/cloudevents+json"
        };

        await sender.SendMessageAsync(serviceBusMessage);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}