// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text.Json;
using Confluent.Kafka;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

namespace Stickerlandia.UserManagement.Agnostic;

[AsyncApi]
public class KafakStickerClaimedWorker : IMessagingWorker
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafakStickerClaimedWorker> _logger;
    private readonly ConsumerConfig _consumerConfig;
    const string topic = "users.stickerClaimed.v1";
    
    public KafakStickerClaimedWorker(ILogger<KafakStickerClaimedWorker> logger, IServiceScopeFactory serviceScopeFactory, ConsumerConfig consumerConfig)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _consumerConfig = consumerConfig;
    }
    
    [Channel("users.stickerClaimed.v1")]
    [SubscribeOperation(typeof(StickerClaimedEventV1))]
    private async Task<bool> ProcessMessageAsync(StickerClaimedEventHandler handler, ConsumeResult<string, string> consumeResult)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");
        
        _logger.LogInformation("Received message: {body}", consumeResult.Message.Value);
    
        var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(consumeResult.Message.Value);
    
        if (evtData == null)
        {
            return false;
        }
        
        // Process your message here
        await handler.Handle(evtData!);
        
        return true;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Starting ServiceBus processor");
        
        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService<StickerClaimedEventHandler>();
        
        using (var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build())
        {
            consumer.Subscribe(topic);
            try {
                var cr = consumer.Consume(stoppingToken);
                
                var processResult = await ProcessMessageAsync(handler, cr);

                if (processResult)
                {
                    consumer.Commit(cr);
                }
            }   
            catch (Exception ex) {
                _logger.LogError(ex, "Error processing message");
            }
            finally{
                consumer.Close();
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // NOOP
    }
}