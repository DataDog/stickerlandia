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

public class Kafka(ILogger<Functions> logger, IServiceScopeFactory serviceScopeFactory, OutboxProcessor outboxProcessor)
{
    [LambdaFunction]
    public async Task StickerClaimed(KafkaEvent kafkaEvent)
    {
        using var processSpan = Tracer.Instance.StartActive($"process users.stickerClaimed.v1");

        using var scope = serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<StickerClaimedEventHandler>();

        foreach (var message in kafkaEvent.Records)
        foreach (var record in message.Value)
        {
            var evtData = JsonSerializer.Deserialize<StickerClaimedEventV1>(record.Value);

            if (evtData == null) continue;

            try
            {
                await handler.Handle(evtData!);
            }
            catch (InvalidUserException ex)
            {
                logger.LogWarning(ex, "User with account in this event not found");
            }
        }
    }

    [LambdaFunction]
    public async Task OutboxWorker(object evtData)
    {
        logger.LogInformation("Running outbox timer");
        
        await outboxProcessor.ProcessAsync();
    }
}