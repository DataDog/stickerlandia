/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class OutboxProcessorTests : IDisposable
{
    private readonly IOutbox _outbox;
    private readonly IPrintServiceEventPublisher _eventPublisher;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly PrintJobInstrumentation _instrumentation;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        _outbox = A.Fake<IOutbox>();
        _eventPublisher = A.Fake<IPrintServiceEventPublisher>();
        _logger = A.Fake<ILogger<OutboxProcessor>>();

        var serviceProvider = A.Fake<IServiceProvider>();
        A.CallTo(() => serviceProvider.GetService(typeof(IOutbox)))
            .Returns(_outbox);
        A.CallTo(() => serviceProvider.GetService(typeof(IPrintServiceEventPublisher)))
            .Returns(_eventPublisher);

        var serviceScope = A.Fake<IServiceScope>();
        A.CallTo(() => serviceScope.ServiceProvider)
            .Returns(serviceProvider);

        _serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        A.CallTo(() => _serviceScopeFactory.CreateScope())
            .Returns(serviceScope);

        _instrumentation = new PrintJobInstrumentation();
        _processor = new OutboxProcessor(_serviceScopeFactory, _logger, _instrumentation);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _instrumentation.Dispose();
        }
    }

    public class ProcessAsyncMethod : OutboxProcessorTests
    {
        [Fact]
        public async Task WithUnprocessedItems_ProcessesAllItems()
        {
            var items = new List<OutboxItem>
            {
                CreateOutboxItem("printers.registered.v1", new PrinterRegisteredEvent { PrinterId = "TEST-1" }),
                CreateOutboxItem("printers.registered.v1", new PrinterRegisteredEvent { PrinterId = "TEST-2" })
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(items);

            await _processor.ProcessAsync();

            A.CallTo(() => _outbox.UpdateOutboxItem(A<OutboxItem>._))
                .MustHaveHappened(2, Times.Exactly);
        }

        [Fact]
        public async Task WithPrinterRegisteredEvent_PublishesEvent()
        {
            var printerEvent = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER" };
            var items = new List<OutboxItem>
            {
                CreateOutboxItem("printers.registered.v1", printerEvent)
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(items);

            await _processor.ProcessAsync();

            A.CallTo(() => _eventPublisher.PublishPrinterRegisteredEvent(
                A<PrinterRegisteredEvent>.That.Matches(e => e.PrinterId == "TESTEVENT-PRINTER")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNoItems_CompletesWithoutError()
        {
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(new List<OutboxItem>());

            var action = async () => await _processor.ProcessAsync();

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task WithUnknownEventType_MarksAsFailed()
        {
            var item = new OutboxItem
            {
                EventType = "unknown.event.type",
                EventData = "{}"
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(new List<OutboxItem> { item });

            await _processor.ProcessAsync();

            item.Failed.Should().BeTrue();
            item.FailureReason.Should().Be("Unknown event type");
        }

        [Fact]
        public async Task WithInvalidJson_MarksAsFailedWithReason()
        {
            var item = new OutboxItem
            {
                EventType = "printers.registered.v1",
                EventData = "not valid json {"
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(new List<OutboxItem> { item });

            await _processor.ProcessAsync();

            item.Failed.Should().BeTrue();
            item.FailureReason.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenPublisherThrows_MarksAsFailedWithMessage()
        {
            var items = new List<OutboxItem>
            {
                CreateOutboxItem("printers.registered.v1", new PrinterRegisteredEvent { PrinterId = "TEST" })
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(items);
            A.CallTo(() => _eventPublisher.PublishPrinterRegisteredEvent(A<PrinterRegisteredEvent>._))
                .Throws(new Exception("Publisher failed"));

            await _processor.ProcessAsync();

            items[0].Failed.Should().BeTrue();
            items[0].FailureReason.Should().Be("Publisher failed");
        }

        [Fact]
        public async Task WithValidEvent_MarksAsProcessed()
        {
            var item = CreateOutboxItem("printers.registered.v1", new PrinterRegisteredEvent { PrinterId = "TEST" });
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(new List<OutboxItem> { item });

            await _processor.ProcessAsync();

            item.Processed.Should().BeTrue();
            item.Failed.Should().BeFalse();
        }

        [Fact]
        public async Task WithMultipleMixedItems_ProcessesEachIndependently()
        {
            var validItem = CreateOutboxItem("printers.registered.v1", new PrinterRegisteredEvent { PrinterId = "VALID" });
            var invalidItem = new OutboxItem
            {
                EventType = "unknown.type",
                EventData = "{}"
            };
            A.CallTo(() => _outbox.GetUnprocessedItemsAsync(100))
                .Returns(new List<OutboxItem> { validItem, invalidItem });

            await _processor.ProcessAsync();

            validItem.Processed.Should().BeTrue();
            invalidItem.Failed.Should().BeTrue();
        }
    }

    private static OutboxItem CreateOutboxItem(string eventType, PrinterRegisteredEvent eventData)
    {
        return new OutboxItem
        {
            EventType = eventType,
            EventData = JsonSerializer.Serialize(eventData)
        };
    }
}

public class OutboxItemTests
{
    public class Constructor
    {
        [Fact]
        public void GeneratesUniqueItemId()
        {
            var item1 = new OutboxItem();
            var item2 = new OutboxItem();

            item1.ItemId.Should().NotBeNullOrEmpty();
            item2.ItemId.Should().NotBeNullOrEmpty();
            item1.ItemId.Should().NotBe(item2.ItemId);
        }

        [Fact]
        public void SetsEventTimeToUtcNow()
        {
            var beforeCreation = DateTime.UtcNow;
            var item = new OutboxItem();
            var afterCreation = DateTime.UtcNow;

            item.EventTime.Should().BeOnOrAfter(beforeCreation);
            item.EventTime.Should().BeOnOrBefore(afterCreation);
        }

        [Fact]
        public void InitializesWithDefaultValues()
        {
            var item = new OutboxItem();

            item.EventType.Should().Be("");
            item.EventData.Should().Be("");
            item.Processed.Should().BeFalse();
            item.Failed.Should().BeFalse();
            item.FailureReason.Should().BeNull();
            item.EmailAddress.Should().Be("");
        }

        [Fact]
        public void ItemIdIsValidGuid()
        {
            var item = new OutboxItem();

            var parseResult = Guid.TryParse(item.ItemId, out _);

            parseResult.Should().BeTrue();
        }
    }
}
