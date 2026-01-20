/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Datadog.Trace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saunter.Attributes;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;
using Log = Stickerlandia.PrintService.Core.Observability.Log;

namespace Stickerlandia.PrintService.AWS;

[AsyncApi]
public class SnsEventPublisher(
    ILogger<SnsEventPublisher> logger,
    AmazonSimpleNotificationServiceClient client,
    IOptions<AwsConfiguration> awsConfiguration) : IPrintServiceEventPublisher
{
    [Channel("users.userRegistered.v1")]
    [PublishOperation(typeof(PrinterRegisteredEvent))]
    public async Task PublishPrinterRegisteredEvent(PrinterRegisteredEvent printerRegisteredEvent)
    {
        ArgumentNullException.ThrowIfNull(printerRegisteredEvent, nameof(printerRegisteredEvent));

        var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://stickerlandia.com"),
            Type = printerRegisteredEvent.EventName,
            Time = DateTime.UtcNow,
            Data = printerRegisteredEvent
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

                // Convert TraceId and SpanId to proper hex format for W3C traceparent
                var traceIdHex = activeSpan.TraceId.ToString("x32", CultureInfo.InvariantCulture).PadLeft(32, '0');
                var spanIdHex = activeSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture).PadLeft(16, '0');
                cloudEvent.SetAttributeFromString("traceparent", $"00-{traceIdHex}-{spanIdHex}-01");
            }

            var formatter = new JsonEventFormatter<PrinterRegisteredEvent>();
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