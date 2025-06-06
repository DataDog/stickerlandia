using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Lambda;

public class OutboxFunctions(ILogger<Sqs> logger, IServiceScopeFactory serviceScopeFactory, OutboxProcessor outboxProcessor)
{
    [LambdaFunction]
    public async Task Worker(object evtData)
    {
        logger.LogInformation("Running outbox timer");
        
        await outboxProcessor.ProcessAsync();
    }
}