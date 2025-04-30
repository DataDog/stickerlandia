// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.FunctionApp;

public class HandleStickerClaimedFunction
{
    private readonly ILogger<HandleStickerClaimedFunction> _logger;
    private readonly StickerClaimedEventHandler _stickerClaimedHandler;

    public HandleStickerClaimedFunction(ILogger<HandleStickerClaimedFunction> logger, StickerClaimedEventHandler stickerClaimedHandler)
    {
        _logger = logger;
        _stickerClaimedHandler = stickerClaimedHandler;
    }

    [Function(nameof(HandleStickerClaimedFunction))]
    public async Task Run(
        [ServiceBusTrigger("users.stickerClaimed.v1", Connection = "ConnectionStrings:messaging")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Successfully received message with body: {Body}", message.Body);

        try
        {
            // Parse the message body to get the event
            var eventData = JsonSerializer.Deserialize<StickerClaimedEventV1>(message.Body);
            
            if (eventData != null)
            {
                // Process the event
                await _stickerClaimedHandler.Handle(eventData);
                
                // Complete the message
                await messageActions.CompleteMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("Failed to deserialize sticker claimed event");
                await messageActions.DeadLetterMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sticker claimed event");
            await messageActions.DeadLetterMessageAsync(message);
        }
    }
}