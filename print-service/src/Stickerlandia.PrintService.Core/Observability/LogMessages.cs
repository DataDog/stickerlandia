/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.Logging;

namespace Stickerlandia.PrintService.Core.Observability;

public static partial class Log
{

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Critical,
        Message = "Received message from transport: {MessageTransport}")]
    public static partial void ReceivedMessage(
        ILogger logger, string messageTransport);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Failure processing message: {ErrorMessage}")]
    public static partial void MessageProcessingException(
        ILogger logger, string? errorMessage, Exception? exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Invalid user exception")]
    public static partial void InvalidUser(
        ILogger logger, InvalidUserException exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failure delivering message: {ErrorReason}")]
    public static partial void MessageDeliveryFailure(
        ILogger logger, string errorReason, Exception? exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Message sent to DLQ: {ErrorReason}")]
    public static partial void MessageSentToDlq(
        ILogger logger, string errorReason, Exception? exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Trace,
        Message = "Starting message processor for transport: {MessageTransport}")]
    public static partial void StartingMessageProcessor(
        ILogger logger, string messageTransport);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Token cancelled")]
    public static partial void TokenCancelled(
        ILogger logger);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Failed to create message processor for transport: {MessageTransport}")]
    public static partial void FailureStartingWorker(
        ILogger logger, string messageTransport, Exception? exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Failure processing message: {ErrorMessage}")]
    public static partial void MessagePublishingError(
        ILogger logger, string? errorMessage, Exception? exception);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Unknown error")]
    public static partial void UnknownException(
        ILogger logger, Exception? exception);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "{Message}")]
    public static partial void GenericWarning(
        ILogger logger, string message, Exception? exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Trace,
        Message = "Stopping message processor for transport: {MessageTransport}")]
    public static partial void StoppingMessageProcessor(
        ILogger logger, string messageTransport);
}

public static class LogMessages
{
    public static readonly Action<ILogger, int, Exception?> LogUnprocessedOutboxItems =
        LoggerMessage.Define<int>(
            LogLevel.Trace,
            new EventId(1, nameof(LogUnprocessedOutboxItems)),
            "There are {Count} unprocessed outbox items");

    public static readonly Action<ILogger, Exception?> LogErrorProcessingOutboxItems =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(LogErrorProcessingOutboxItems)),
            "Error processing outbox items");

    public static readonly Action<ILogger, string, Exception?> LogOutboxItemDeserializationWarning =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(3, nameof(LogOutboxItemDeserializationWarning)),
            "Contents of outbox item cannot be deserialized {ItemId}");

    public static readonly Action<ILogger, string, Exception?> LogFailureProcessingOutboxItem =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(4, nameof(LogFailureProcessingOutboxItem)),
            "Failure processing outbox item {ItemId}");
}