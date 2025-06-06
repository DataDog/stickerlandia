using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.KafkaEvents;
using Amazon.Lambda.SQSEvents;
using Datadog.Trace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;
using Stickerlandia.UserManagement.Core.StickerClaimedEvent;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Stickerlandia.UserManagement.Lambda;

public class Functions(ILogger<Functions> logger, IServiceScopeFactory serviceScopeFactory, OutboxProcessor outboxProcessor)
{
    [LambdaFunction]
    public async Task<SQSBatchResponse> StickerClaimed(SQSEvent sqsEvent)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedEventHandler>();

        var failedMessages = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in sqsEvent.Records) await ProcessMessage(message, failedMessages, handler);

        return new SQSBatchResponse(failedMessages);
    }

    private async Task ProcessMessage(SQSEvent.SQSMessage message,
        List<SQSBatchResponse.BatchItemFailure> failedMessages,
        StickerClaimedEventHandler handler)
    {
        var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(message.Body);

        if (evtData == null)
        {
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
            return;
        }

        try
        {
            await handler.Handle(evtData!);
        }
        catch (InvalidUserException ex)
        {
            logger.LogWarning(ex, "User with account in this event not found");
            failedMessages.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
        }
    }

    [LambdaFunction]
    public async Task OutboxWorker(object evtData)
    {
        logger.LogInformation("Running outbox timer");
        
        await outboxProcessor.ProcessAsync();
    }
}