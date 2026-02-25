/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.Azure;

[AsyncApi]
public class ServiceBusStickerPrintedWorker : IMessagingWorker
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ServiceBusStickerPrintedWorker> _logger;
    private readonly ServiceBusProcessor _processor;

    public ServiceBusStickerPrintedWorker(ILogger<ServiceBusStickerPrintedWorker> logger,
        ServiceBusClient serviceBusClient, IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceBusClient, nameof(serviceBusClient));
        
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        // Create the processor
        _processor = serviceBusClient.CreateProcessor("printJobs.completed.v1");

        // Set up handlers
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    [Channel("printJobs.completed.v1")]
    [SubscribeOperation(typeof(StickerClaimedEventV1))]
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        using var processSpan = Tracer.Instance.StartActive($"process printJobs.completed.v1");

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerPrintedHandler>();

        var messageBody = args.Message.Body.ToString();
        Log.ReceivedMessage(_logger, "ServiceBus");

        var detailBytes = JsonSerializer.SerializeToUtf8Bytes(messageBody, _jsonSerializerOptions);
        var formatter = new JsonEventFormatter<StickerPrintedEventV1>();
        var cloudEvent = await formatter.DecodeStructuredModeMessageAsync(
            new MemoryStream(detailBytes), null, new List<CloudEventAttribute>());
        var stickerPrinted = (StickerPrintedEventV1?)cloudEvent.Data;

        if (stickerPrinted == null) await args.DeadLetterMessageAsync(args.Message, "Message body cannot be deserialized");
        
        try
        {
            await handler.Handle(stickerPrinted!);
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