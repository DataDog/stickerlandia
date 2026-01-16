/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

#pragma warning disable CA1812

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Grpc.Core;

namespace Stickerlandia.PrintService.IntegrationTest.Drivers;

internal sealed class GooglePubSubMessaging : IMessaging, IAsyncDisposable
{
    private readonly PublisherClient _client;

    public GooglePubSubMessaging(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string must be provided for Google Pub/Sub messaging.",
                nameof(connectionString));

        var topicName = new TopicName(connectionString, "users.userRegistered.v1");
        var stickerClaimedTopic = new TopicName(connectionString, "users.stickerClaimed.v1");
            
        var publisherApiClient = new PublisherServiceApiClientBuilder { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.Build();
        
        try
        {
            publisherApiClient.CreateTopic(topicName);
            Console.WriteLine($"Topic {topicName} created.");
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"Topic {topicName} already exists.");
        }
        
        try
        {
            publisherApiClient.CreateTopic(stickerClaimedTopic);
            Console.WriteLine($"Topic {stickerClaimedTopic} created.");
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
        {
            Console.WriteLine($"Topic {stickerClaimedTopic} already exists.");
        }
        
        var subscriber = new SubscriberServiceApiClientBuilder() { EmulatorDetection = EmulatorDetection.EmulatorOrProduction }.Build();
        SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(connectionString, "users.stickerClaimed.v1");
        
        try
        {
            subscriber.CreateSubscription(subscriptionName, stickerClaimedTopic, pushConfig: null, ackDeadlineSeconds: 60);
        }
        catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
        {
            // Already exists.  That's fine.
        }

        _client = new PublisherClientBuilder { TopicName = stickerClaimedTopic, EmulatorDetection = EmulatorDetection.EmulatorOrProduction}.Build();
    }

    public async Task SendMessageAsync(string queueName, object message)
    {
        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = queueName,
            Time = DateTime.UtcNow,
            Data = message
        };

        var formatter = new JsonEventFormatter<object>();
        var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);

        var publishResult = await _client.PublishAsync(ByteString.CopyFrom(data.Span));
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}