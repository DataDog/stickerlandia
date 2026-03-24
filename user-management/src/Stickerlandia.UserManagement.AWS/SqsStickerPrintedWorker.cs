/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

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
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;

namespace Stickerlandia.UserManagement.AWS;

[AsyncApi]
public class SqsStickerPrintedWorker : IMessagingWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SqsStickerPrintedWorker> _logger;
    private readonly AmazonSQSClient _sqsClient;
    private readonly IOptions<AwsConfiguration> _awsConfiguration;

    public SqsStickerPrintedWorker(ILogger<SqsStickerPrintedWorker> logger,
        AmazonSQSClient sqsClient, IServiceScopeFactory serviceScopeFactory,
        IOptions<AwsConfiguration> awsConfiguration)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _serviceScopeFactory = serviceScopeFactory;
        _awsConfiguration = awsConfiguration;
    }

    [Channel("printJobs.completed.v1")]
    [SubscribeOperation(typeof(StickerPrintedEventV1))]
    private async Task ProcessMessageAsync(Message message)
    {
        var extractedContext = new SpanContextExtractor().ExtractIncludingDsm(
            message.MessageAttributes,
            static (attributes, key) =>
            {
                if (attributes.TryGetValue(key, out var attr))
                    return new[] { attr.StringValue };
                return Enumerable.Empty<string>();
            },
            "sqs",
            "printJobs.completed.v1");

        using var processSpan = Tracer.Instance.StartActive($"process printJobs.completed.v1",
            new SpanCreationSettings { Parent = extractedContext });

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerPrintedHandler>();

        var evtData = JsonSerializer.Deserialize<StickerPrintedEventV1>(message.Body);

        if (evtData == null)
        {
            await _sqsClient.SendMessageAsync(_awsConfiguration.Value.StickerPrintedDLQUrl, message.Body);
            await _sqsClient.DeleteMessageAsync(_awsConfiguration.Value.StickerPrintedQueueUrl, message.ReceiptHandle);
            return;
        }

        // Process your message here
        try
        {
            await handler.Handle(evtData!);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(_logger, ex);
            
            await _sqsClient.SendMessageAsync(_awsConfiguration.Value.StickerPrintedDLQUrl, message.Body);
        }
        
        await _sqsClient.DeleteMessageAsync(_awsConfiguration.Value.StickerPrintedQueueUrl, message.ReceiptHandle);
    }

    public Task StartAsync()
    {
        Log.StartingMessageProcessor(_logger, "sqs");
        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = _awsConfiguration.Value.StickerPrintedQueueUrl,
            WaitTimeSeconds = 20,
            MaxNumberOfMessages = 10,
            MessageAttributeNames = new List<string> { "All" }
        };

        var messages = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);
        foreach (var message in messages.Messages) await ProcessMessageAsync(message);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.StoppingMessageProcessor(_logger, "sqs");
        // No specific stop logic for SQS, as it is a pull-based system.
        await Task.CompletedTask;
    }
}