/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1812

using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class GooglePubSubMessaging : IMessaging, IAsyncDisposable
{
    private readonly Dictionary<string, PublisherClient> _clients = new();

    public GooglePubSubMessaging(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string must be provided for Google Pub/Sub messaging.",
                nameof(connectionString));

        var publisherApiClient = new PublisherServiceApiClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.Build();
        var subscriber = new SubscriberServiceApiClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.Build();

        var topics = new[]
        {
            "users.userRegistered.v1",
            "users.stickerClaimed.v1",
            "printJobs.completed.v1"
        };

        foreach (var topicId in topics)
        {
            var topicName = new TopicName(connectionString, topicId);

            try
            {
                publisherApiClient.CreateTopic(topicName);
                Console.WriteLine($"Topic {topicName} created.");
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                Console.WriteLine($"Topic {topicName} already exists.");
            }

            var subscriptionName = SubscriptionName.FromProjectSubscription(connectionString, topicId);
            try
            {
                subscriber.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
            {
                // Already exists. That's fine.
            }

            _clients[topicId] = new PublisherClientBuilder
            {
                TopicName = topicName,
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction
            }.Build();
        }
    }

    public async Task SendMessageAsync(string queueName, string messageJson)
    {
        if (!_clients.TryGetValue(queueName, out var client))
            throw new InvalidOperationException($"No publisher client configured for topic '{queueName}'");

        await client.PublishAsync(ByteString.CopyFromUtf8(messageJson));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
            await client.DisposeAsync();
    }
}