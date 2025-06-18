// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Azure;

[AsyncApi]
public class ServiceBusStickerClaimedWorker : IMessagingWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ServiceBusStickerClaimedWorker> _logger;
    private readonly ServiceBusProcessor _processor;

    public ServiceBusStickerClaimedWorker(ILogger<ServiceBusStickerClaimedWorker> logger,
        ServiceBusClient serviceBusClient, IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceBusClient, nameof(serviceBusClient));
        
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        // Create the processor
        _processor = serviceBusClient.CreateProcessor("users.stickerClaimed.v1");

        // Set up handlers
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    [Channel("users.stickerClaimed.v1")]
    [SubscribeOperation(typeof(StickerClaimedEventV1))]
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedHandler>();

        var messageBody = args.Message.Body.ToString();
        Log.ReceivedMessage(_logger, "ServiceBus");

        var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(messageBody);

        if (evtData == null) await args.DeadLetterMessageAsync(args.Message, "Message body cannot be deserialized");

        // Process your message here
        try
        {
            await handler.Handle(evtData!);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(_logger, ex);
            await args.DeadLetterMessageAsync(args.Message, "Invalid account id");
        }

        // Complete the message
        await args.CompleteMessageAsync(args.Message, CancellationToken.None);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Log.MessageProcessingException(_logger, args.ErrorSource.ToString(), null);
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        Log.StartingMessageProcessor(_logger, "ServiceBus");
        _processor.StartProcessingAsync();
        return Task.CompletedTask;
    }

    public Task PollAsync(CancellationToken stoppingToken)
    {
        // This should be a no-op;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.CloseAsync(cancellationToken);
    }
}