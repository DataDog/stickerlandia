using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Observability;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Lambda;

public class OutboxFunctions(ILogger<SqsHandler> logger, OutboxProcessor outboxProcessor)
{
    [LambdaFunction]
    public async Task Worker(object evtData)
    {
        Log.StartingMessageProcessor(logger, "outbox-worker");
        
        await outboxProcessor.ProcessAsync();
    }
}