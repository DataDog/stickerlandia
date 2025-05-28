// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Confluent.Kafka;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public class KafkaMessaging : IMessaging, IAsyncDisposable
{
    private readonly ProducerConfig config;
    public KafkaMessaging(string connectionString)
    {
        config = new ProducerConfig
        {
            // User-specific properties that you must set
            BootstrapServers = connectionString,
            // Fixed properties
            SecurityProtocol = SecurityProtocol.Plaintext,
            Acks             = Acks.All
        };
    }
    public Task SendMessageAsync(string queueName, object message)
    {
        using var producer = new ProducerBuilder<string, string>(config).Build();
            
        producer.Produce(queueName, new Message<string, string> { Key = "", Value = JsonSerializer.Serialize(message) },
            (deliveryReport) =>
            {
                if (deliveryReport.Error.Code != ErrorCode.NoError) {
                    throw new Exception($"Error publishing message to Kafka: {deliveryReport.Error.Reason}");
                }
                else {
                    Console.WriteLine($"Message sent to Kafka topic {queueName} with offset {deliveryReport.Offset}");
                }
            });
                
        producer.Flush(TimeSpan.FromSeconds(10));

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Nothing to dispose
    }
}