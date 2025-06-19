// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.Attributes;
using Stickerlandia.UserManagement.Agnostic.Observability;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Log = Stickerlandia.UserManagement.Core.Observability.Log;

namespace Stickerlandia.UserManagement.AWS;

[AsyncApi]
public class SnsEventPublisher(
    ILogger<SnsEventPublisher> logger,
    AmazonSimpleNotificationServiceClient client,
    IOptions<AwsConfiguration> awsConfiguration) : IUserEventPublisher
{
    [Channel("users.userRegistered.v1")]
    [PublishOperation(typeof(UserRegisteredEvent))]
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

        await Publish(cloudEvent);
    }

    private async Task Publish(CloudEvent cloudEvent)
    {
        var activeSpan = Tracer.Instance.ActiveScope?.Span;
        IScope? processScope = null;

        try
        {
            if (activeSpan != null)
            {
                processScope = Tracer.Instance.StartActive($"publish {cloudEvent.Type}", new SpanCreationSettings
                {
                    Parent = activeSpan.Context
                });

                cloudEvent.SetAttributeFromString("traceparent", $"00-{activeSpan.TraceId}-{activeSpan.SpanId}-01");
            }

            var formatter = new JsonEventFormatter<UserRegisteredEvent>();
            var data = formatter.EncodeStructuredModeMessage(cloudEvent, out _);
            var jsonString = System.Text.Encoding.UTF8.GetString(data.Span);

            await client.PublishAsync(new PublishRequest(awsConfiguration.Value.UserRegisteredTopicArn, jsonString));
        }
        catch (Exception ex)
        {
            Log.MessagePublishingError(logger, "Failure publishing event", ex);
            processScope?.Span.SetException(ex);
            throw;
        }
        finally
        {
            processScope?.Close();
        }
    }
}