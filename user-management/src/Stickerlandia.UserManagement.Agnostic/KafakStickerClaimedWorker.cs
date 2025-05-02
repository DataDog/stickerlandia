// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.Agnostic;

[AsyncApi]
public class KafakStickerClaimedWorker : IMessagingWorker
{
    private readonly StickerClaimedEventHandler _eventHandler;
    private readonly ILogger<KafakStickerClaimedWorker> _logger;

    public KafakStickerClaimedWorker(StickerClaimedEventHandler eventHandler, ILogger<KafakStickerClaimedWorker> logger)
    {
        _eventHandler = eventHandler;
        _logger = logger;
    }
    
    // [Channel("users.stickerClaimed.v1")]
    // [SubscribeOperation(typeof(StickerClaimedEventV1))]
    // private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    // {
    //     using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");
    //     
    //     var messageBody = args.Message.Body.ToString();
    //     _logger.LogInformation("Received message: {body}", messageBody);
    //
    //     var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(messageBody);
    //
    //     if (evtData == null)
    //     {
    //         await args.DeadLetterMessageAsync(args.Message, "Message body cannot be deserialized");
    //     }
    //     
    //     // Process your message here
    //     await _eventHandler.Handle(evtData!);
    //     
    //     // Complete the message
    //     await args.CompleteMessageAsync(args.Message, CancellationToken.None);
    // }
    
    // private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    // {
    //     _logger.LogError(args.Exception, "Error processing message: {source}", args.ErrorSource);
    //     return Task.CompletedTask;
    // }

    public Task StartAsync()
    {
        _logger.LogInformation("Starting ServiceBus processor");
        return Task.CompletedTask;
    }

    public Task PollAsync()
    {
        // This should be a no-op;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}