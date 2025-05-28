// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Text;
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
public class KafakStickerClaimedWorker(
    ILogger<KafakStickerClaimedWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    ConsumerConfig consumerConfig,
    ProducerConfig producerConfig)
    : IMessagingWorker
{
    private readonly ProducerConfig _producerConfig = producerConfig;
    private const string topic = "users.stickerClaimed.v1";
    private const string dlqTopic = "users.stickerClaimed.v1.dlq";

    [Channel("users.stickerClaimed.v1")]
    [SubscribeOperation(typeof(StickerClaimedEventV1))]
    private async Task<bool> ProcessMessageAsync(StickerClaimedEventHandler handler,
        ConsumeResult<string, string> consumeResult)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        logger.LogInformation("Received message: {body}", consumeResult.Message.Value);

        var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(consumeResult.Message.Value);

        if (evtData == null) return false;

        try
        {
            await handler.Handle(evtData!);
        }
        catch (InvalidUserException ex)
        {
            logger.LogWarning("Invalid user in sticker claimed event: {Message}", ex.Message);

            SendToDLQ(consumeResult, evtData);
        }

        return true;
    }

    private void SendToDLQ(ConsumeResult<string, string> consumeResult, StickerClaimedEventV1 evtData)
    {
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        producer.Produce(dlqTopic,
            new Message<string, string> { Key = evtData.AccountId, Value = consumeResult.Message.Value },
            (deliveryReport) =>
            {
                if (deliveryReport.Error.Code != ErrorCode.NoError)
                    logger.LogError($"Failed to deliver message: {deliveryReport.Error.Reason}");
                else
                    logger.LogWarning($"Produced event to topic {dlqTopic}");
            });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    public Task StartAsync()
    {
        logger.LogInformation("Starting Kafka processor");

        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        // Check for cancellation first
        if (stoppingToken.IsCancellationRequested)
            return;

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedEventHandler>();

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, e) => logger.LogError("Kafka error: {Error}", e.Reason))
                .Build();

            consumer.Subscribe(topic);

            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));

                if (consumeResult != null)
                {
                    var processResult = await ProcessMessageAsync(handler, consumeResult);

                    if (processResult) consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Error consuming message");
            }
            catch (OperationCanceledException)
            {
                // This is expected when the token is canceled
                logger.LogInformation("Polling canceled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");
            }
            finally
            {
                // Ensure consumer is closed even if an exception occurs
                consumer.Close();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Kafka consumer");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // NOOP
        return Task.CompletedTask;
    }
}