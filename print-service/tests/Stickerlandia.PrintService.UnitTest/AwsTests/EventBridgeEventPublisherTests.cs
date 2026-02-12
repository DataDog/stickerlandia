// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.AWS;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.AwsTests;

public class EventBridgeEventPublisherTests
{
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly EventBridgeEventPublisher _publisher;

    public EventBridgeEventPublisherTests()
    {
        _eventBridgeClient = A.Fake<IAmazonEventBridge>();
        var logger = A.Fake<ILogger<EventBridgeEventPublisher>>();
        var options = A.Fake<IOptions<AwsConfiguration>>();
        A.CallTo(() => options.Value).Returns(new AwsConfiguration { EventBusName = "test-bus" });

        _publisher = new EventBridgeEventPublisher(logger, _eventBridgeClient, options);
    }

    [Fact]
    public async Task PublishPrinterRegisteredEvent_WhenPutEventsSucceeds_DoesNotThrow()
    {
        var response = new PutEventsResponse
        {
            FailedEntryCount = 0,
            Entries = [new PutEventsResultEntry { EventId = "evt-1" }]
        };
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Returns(response);

        var evt = new PrinterRegisteredEvent { PrinterId = "TEST-PRINTER" };

        var act = async () => await _publisher.PublishPrinterRegisteredEvent(evt);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishPrinterRegisteredEvent_WhenPartialFailure_ThrowsException()
    {
        var response = new PutEventsResponse
        {
            FailedEntryCount = 1,
            Entries =
            [
                new PutEventsResultEntry
                {
                    ErrorCode = "InternalFailure",
                    ErrorMessage = "Something went wrong"
                }
            ]
        };
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Returns(response);

        var evt = new PrinterRegisteredEvent { PrinterId = "TEST-PRINTER" };

        var act = async () => await _publisher.PublishPrinterRegisteredEvent(evt);

        await act.Should().ThrowAsync<EventBridgePartialFailureException>();
    }

    [Fact]
    public async Task PublishPrintJobQueuedEvent_WhenPartialFailure_ThrowsException()
    {
        var response = new PutEventsResponse
        {
            FailedEntryCount = 1,
            Entries =
            [
                new PutEventsResultEntry
                {
                    ErrorCode = "ThrottlingException",
                    ErrorMessage = "Rate exceeded"
                }
            ]
        };
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Returns(response);

        var printJob = PrintJob.Create(
            new PrinterId("TESTEVENT-TESTPRINTER"),
            "user-1",
            "sticker-1",
            "https://example.com/sticker.png");
        var evt = new PrintJobQueuedEvent(printJob);

        var act = async () => await _publisher.PublishPrintJobQueuedEvent(evt);

        await act.Should().ThrowAsync<EventBridgePartialFailureException>();
    }

    [Fact]
    public async Task PublishPrinterRegisteredEvent_WhenPartialFailure_ExceptionContainsFailedEntryDetails()
    {
        var response = new PutEventsResponse
        {
            FailedEntryCount = 1,
            Entries =
            [
                new PutEventsResultEntry
                {
                    ErrorCode = "InternalFailure",
                    ErrorMessage = "Something went wrong"
                }
            ]
        };
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Returns(response);

        var evt = new PrinterRegisteredEvent { PrinterId = "TEST-PRINTER" };

        var act = async () => await _publisher.PublishPrinterRegisteredEvent(evt);

        var exception = await act.Should().ThrowAsync<EventBridgePartialFailureException>();
        exception.Which.Message.Should().Contain("1");
        exception.Which.Message.Should().Contain("InternalFailure");
    }
}
