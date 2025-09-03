using System.Globalization;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;

namespace Stickerlandia.UserManagement.GCP;

public class GooglePubSubEventPublisher([FromKeyedServices("users.userRegistered.v1")] PublisherClient publisherClient)  : IUserEventPublisher
{
    public async Task PublishUserRegisteredEventV1(UserRegisteredEvent userRegisteredEvent)
    {
        ArgumentNullException.ThrowIfNull(userRegisteredEvent, nameof(userRegisteredEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = userRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = userRegisteredEvent
        };
        
        var activeSpan = Tracer.Instance.ActiveScope?.Span;
        IScope? publishScope = null;
        
        if (activeSpan != null)
        {
            publishScope = Tracer.Instance.StartActive($"publish {cloudEvent.Type}", new SpanCreationSettings
            {
                Parent = activeSpan.Context
            });

            // Convert TraceId and SpanId to proper hex format for W3C traceparent
            var traceIdHex = activeSpan.TraceId.ToString("x32", CultureInfo.InvariantCulture).PadLeft(32, '0');
            var spanIdHex = activeSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture).PadLeft(16, '0');
            cloudEvent.SetAttributeFromString("traceparent", $"00-{traceIdHex}-{spanIdHex}-01");
        }
        
        var formatter = new JsonEventFormatter<UserRegisteredEvent>();
        var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);
        
        await publisherClient.PublishAsync(ByteString.CopyFrom(data.Span));
    }
}