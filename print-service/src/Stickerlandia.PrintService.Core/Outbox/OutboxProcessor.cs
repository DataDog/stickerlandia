/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Catching generic exceptions is not recommended, but in this case we want to catch all exceptions so that a failure in outbox processing does not crash the application.
#pragma warning disable CA1031
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.Core.Outbox;

public class OutboxProcessor(IServiceScopeFactory serviceScope, ILogger<OutboxProcessor> logger, PrintJobInstrumentation instrumentation)
{
    public async Task ProcessAsync()
    {
        using var scope = serviceScope.CreateScope();
        IOutbox outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        IPrintServiceEventPublisher eventPublisher = scope.ServiceProvider.GetRequiredService<IPrintServiceEventPublisher>();

        using var activity = PrintJobInstrumentation.StartOutboxProcessingActivity();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var outboxItems = await outbox.GetUnprocessedItemsAsync();

            activity?.SetTag("outbox.items.count", outboxItems.Count);

            foreach (var item in outboxItems)
            {
                await ProcessOutboxItemAsync(outbox, eventPublisher, item);
            }

            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            instrumentation.RecordOutboxProcessingDuration(stopwatch.Elapsed, outboxItems.Count);

            LogMessages.LogUnprocessedOutboxItems(logger, 5, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            instrumentation.RecordOutboxProcessingDuration(stopwatch.Elapsed, 0);
            LogMessages.LogErrorProcessingOutboxItems(logger, ex);
        }
    }

    private async Task ProcessOutboxItemAsync(IOutbox outbox, IPrintServiceEventPublisher eventPublisher, OutboxItem item)
    {
        using var activity = PrintJobInstrumentation.StartOutboxItemActivity(item.EventType, item.ItemId);

        try
        {
            switch (item.EventType)
            {
                case "printers.registered.v1":
                    var userRegisteredEvent =
                        JsonSerializer.Deserialize<PrinterRegisteredEvent>(item.EventData);
                    if (userRegisteredEvent == null)
                    {
                        LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
                        item.FailureReason = "Contents of outbox item cannot be deserialized.";
                        item.Failed = true;
                        activity?.SetTag("outbox.item.error", item.FailureReason);
                        activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                        instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                        break;
                    }

                    await eventPublisher.PublishPrinterRegisteredEvent(userRegisteredEvent);
                    item.Processed = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    instrumentation.RecordOutboxItemProcessed(item.EventType);
                    break;
                case "printJobs.queued.v1":
                    var printJobQueuedEvent =
                        JsonSerializer.Deserialize<PrintJobQueuedEvent>(item.EventData);
                    if (printJobQueuedEvent == null)
                    {
                        LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
                        item.FailureReason = "Contents of outbox item cannot be deserialized.";
                        item.Failed = true;
                        activity?.SetTag("outbox.item.error", item.FailureReason);
                        activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                        instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                        break;
                    }

                    await eventPublisher.PublishPrintJobQueuedEvent(printJobQueuedEvent);
                    item.Processed = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    instrumentation.RecordOutboxItemProcessed(item.EventType);
                    break;
                case "printJobs.failed.v1":
                    var printJobFailedEvent =
                        JsonSerializer.Deserialize<PrintJobFailedEvent>(item.EventData);
                    if (printJobFailedEvent == null)
                    {
                        LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
                        item.FailureReason = "Contents of outbox item cannot be deserialized.";
                        item.Failed = true;
                        activity?.SetTag("outbox.item.error", item.FailureReason);
                        activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                        instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                        break;
                    }

                    await eventPublisher.PublishPrintJobFailedEvent(printJobFailedEvent);
                    item.Processed = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    instrumentation.RecordOutboxItemProcessed(item.EventType);
                    break;
                case "printJobs.completed.v1":
                    var printJobCompletedEvent =
                        JsonSerializer.Deserialize<PrintJobCompletedEvent>(item.EventData);
                    if (printJobCompletedEvent == null)
                    {
                        LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
                        item.FailureReason = "Contents of outbox item cannot be deserialized.";
                        item.Failed = true;
                        activity?.SetTag("outbox.item.error", item.FailureReason);
                        activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                        instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                        break;
                    }

                    await eventPublisher.PublishPrintJobCompletedEvent(printJobCompletedEvent);
                    item.Processed = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    instrumentation.RecordOutboxItemProcessed(item.EventType);
                    break;
                case "printers.deleted.v1":
                    var printerDeletedEvent =
                        JsonSerializer.Deserialize<PrinterDeletedEvent>(item.EventData);
                    if (printerDeletedEvent == null)
                    {
                        LogMessages.LogOutboxItemDeserializationWarning(logger, item.ItemId, null);
                        item.FailureReason = "Contents of outbox item cannot be deserialized.";
                        item.Failed = true;
                        activity?.SetTag("outbox.item.error", item.FailureReason);
                        activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                        instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                        break;
                    }

                    await eventPublisher.PublishPrinterDeletedEvent(printerDeletedEvent);
                    item.Processed = true;
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    instrumentation.RecordOutboxItemProcessed(item.EventType);
                    break;
                default:
                    item.Failed = true;
                    item.FailureReason = "Unknown event type";
                    activity?.SetTag("outbox.item.error", item.FailureReason);
                    activity?.SetStatus(ActivityStatusCode.Error, item.FailureReason);
                    instrumentation.RecordOutboxItemFailed(item.EventType, item.FailureReason);
                    break;
            }
        }
        catch (Exception e)
        {
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.SetTag("error.type", e.GetType().Name);

            LogMessages.LogFailureProcessingOutboxItem(logger, item.ItemId, e);
            item.FailureReason = e.Message;
            item.Failed = true;
            instrumentation.RecordOutboxItemFailed(item.EventType, e.Message);
        }

        await outbox.UpdateOutboxItem(item);
    }
}