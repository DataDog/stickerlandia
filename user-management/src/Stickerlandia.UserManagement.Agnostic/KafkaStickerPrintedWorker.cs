/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Catching generic exceptions is not recommended, but in this case we want to catch all exceptions so that a failure in outbox processing does not crash the application.
#pragma warning disable CA1031

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.StickerPrintedEvent;

namespace Stickerlandia.UserManagement.Agnostic;

[AsyncApi]
public class KafkaStickerPrintedWorker(
    ILogger<KafkaStickerPrintedWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    ConsumerConfig consumerConfig,
    ProducerConfig producerConfig,
    IKafkaConsumerFactory consumerFactory)
    : IMessagingWorker
{
    private const string topic = "printJobs.completed.v1";
    private const string dlqTopic = "printJobs.completed.v1.dlq";

    private IConsumer<string, string>? _consumer;

    [Channel("printJobs.completed.v1")]
    [SubscribeOperation(typeof(StickerPrintedEventV1))]
    private async Task<bool> ProcessMessageAsync(StickerPrintedHandler handler,
        ConsumeResult<string, string> consumeResult)
    {
        using var processSpan = Tracer.Instance.StartActive($"printJobs.completed.v1");

        Log.ReceivedMessage(logger, "kafka");

        var detailBytes = System.Text.Encoding.UTF8.GetBytes(consumeResult.Message.Value);
        var formatter = new JsonEventFormatter<StickerPrintedEventV1>();
        var cloudEvent = await formatter.DecodeStructuredModeMessageAsync(
            new MemoryStream(detailBytes), null, new List<CloudEventAttribute>());
        var stickerPrinted = (StickerPrintedEventV1?)cloudEvent.Data;

        if (stickerPrinted == null) return false;

        try
        {
            await handler.Handle(stickerPrinted);
        }
        catch (InvalidUserException ex)
        {
            Log.InvalidUser(logger, ex);

            SendToDLQ(consumeResult, stickerPrinted);
        }

        return true;
    }

    private void SendToDLQ(ConsumeResult<string, string> consumeResult, StickerPrintedEventV1 evtData)
    {
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        producer.Produce(dlqTopic,
            new Message<string, string> { Key = evtData.UserId, Value = consumeResult.Message.Value },
            (deliveryReport) =>
            {
                if (deliveryReport.Error.Code != ErrorCode.NoError)
                    Log.MessageDeliveryFailure(logger, deliveryReport.Error.Reason, null);
                else
                    Log.MessageSentToDlq(logger, "", null);
            });

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    public Task StartAsync()
    {
        Log.StartingMessageProcessor(logger, "kafka");

        _consumer = consumerFactory.Create(consumerConfig);
        _consumer.Subscribe(topic);

        return Task.CompletedTask;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            var consumeResult = _consumer!.Consume(TimeSpan.FromSeconds(2));

            if (consumeResult != null)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<StickerPrintedHandler>();

                var processResult = await ProcessMessageAsync(handler, consumeResult);

                if (processResult) _consumer.Commit(consumeResult);
            }
        }
        catch (ConsumeException ex)
        {
            Log.MessageProcessingException(logger, "Error consuming message", ex);
        }
        catch (OperationCanceledException)
        {
            Log.TokenCancelled(logger);
        }
        catch (Exception ex)
        {
            Log.MessageProcessingException(logger, "Error consuming message", ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Close();
        _consumer?.Dispose();
        _consumer = null;
        return Task.CompletedTask;
    }
}
