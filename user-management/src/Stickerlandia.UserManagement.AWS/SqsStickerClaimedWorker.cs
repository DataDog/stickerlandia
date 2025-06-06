// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.AWS;

[AsyncApi]
public class SqsStickerClaimedWorker : IMessagingWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SqsStickerClaimedWorker> _logger;
    private readonly AmazonSQSClient _sqsClient;
    private readonly IOptions<AwsConfiguration> _awsConfiguration;

    public SqsStickerClaimedWorker(ILogger<SqsStickerClaimedWorker> logger,
        AmazonSQSClient sqsClient, IServiceScopeFactory serviceScopeFactory,
        IOptions<AwsConfiguration> awsConfiguration)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _serviceScopeFactory = serviceScopeFactory;
        _awsConfiguration = awsConfiguration;
    }

    [Channel("users.stickerClaimed.v1")]
    [SubscribeOperation(typeof(StickerClaimedEventV1))]
    private async Task ProcessMessageAsync(Message message)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedEventHandler>();

        var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(message.Body);

        if (evtData == null)
        {
            await _sqsClient.SendMessageAsync(_awsConfiguration.Value.StickerClaimedDLQUrl, message.Body);
            await _sqsClient.DeleteMessageAsync(_awsConfiguration.Value.StickerClaimedQueueUrl, message.ReceiptHandle);
            return;
        }

        // Process your message here
        try
        {
            await handler.Handle(evtData!);
        }
        catch (InvalidUserException ex)
        {
            _logger.LogWarning(ex, "User with account in this event not found");
            await _sqsClient.SendMessageAsync(_awsConfiguration.Value.StickerClaimedDLQUrl, message.Body);
        }
        
        await _sqsClient.DeleteMessageAsync(_awsConfiguration.Value.StickerClaimedQueueUrl, message.ReceiptHandle);
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Starting SQS processor");
        
        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
        var messages =
            await _sqsClient.ReceiveMessageAsync(_awsConfiguration.Value.StickerClaimedQueueUrl, cancellationToken);

        foreach (var message in messages.Messages) await ProcessMessageAsync(message);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SQS processor");
        // No specific stop logic for SQS, as it is a pull-based system.
        await Task.CompletedTask;
    }
}