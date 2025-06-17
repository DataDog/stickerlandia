// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Amazon.SQS;
using Confluent.Kafka;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public class SqsMessaging : IMessaging, IAsyncDisposable
{
    private readonly AmazonSQSClient client;
    private readonly Dictionary<string, string> queueUrlNameMap;
    public SqsMessaging(Dictionary<string, string> queueUrlNameMap)
    {
        client = new AmazonSQSClient();
        this.queueUrlNameMap = queueUrlNameMap;
    }
    public async Task SendMessageAsync(string queueName, object message)
    {
        if (!queueUrlNameMap.TryGetValue(queueName, out var queueUrl))
        {
            throw new ArgumentException($"Queue name '{queueName}' not found in the queue URL map.");
        }
        
        await client.SendMessageAsync(queueUrl, JsonSerializer.Serialize(message));
    }

    public async ValueTask DisposeAsync()
    {
        // Nothing to dispose
    }
}