// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.Azure;

public class ServiceBusStickerClaimedWorker : IMessagingWorker
{
    private readonly StickerClaimedEventHandler _eventHandler;
    private readonly ILogger<ServiceBusStickerClaimedWorker> _logger;
    private readonly ServiceBusProcessor _processor;

    public ServiceBusStickerClaimedWorker(StickerClaimedEventHandler eventHandler, ILogger<ServiceBusStickerClaimedWorker> logger, ServiceBusClient serviceBusClient)
    {
        _eventHandler = eventHandler;
        _logger = logger;
        
        // Create the processor
        _processor = serviceBusClient.CreateProcessor("users.stickerClaimed.v1");
        
        // Set up handlers
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }
    
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        _logger.LogInformation("Received message: {body}", messageBody);
        
        // Process your message here
        await _eventHandler.Handle(JsonSerializer.Deserialize<StickerClaimedEventV1>(messageBody));
        
        // Complete the message
        await args.CompleteMessageAsync(args.Message, CancellationToken.None);
    }
    
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing message: {source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Starting ServiceBus processor");
        _processor.StartProcessingAsync();
        return Task.CompletedTask;
    }

    public Task PollAsync()
    {
        // This should be a no-op;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync();
        await _processor.CloseAsync(cancellationToken);
    }
}