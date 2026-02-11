/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.GetPrinters;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class GetPrinterStatusesQueryHandlerTests : IDisposable
{
    private readonly IPrinterRepository _repository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly PrintJobInstrumentation _instrumentation;
    private readonly GetPrinterStatusesQueryHandler _handler;

    public GetPrinterStatusesQueryHandlerTests()
    {
        _repository = A.Fake<IPrinterRepository>();
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _instrumentation = new PrintJobInstrumentation();
        _handler = new GetPrinterStatusesQueryHandler(_repository, _printJobRepository, _instrumentation);
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

    public class HandleMethod : GetPrinterStatusesQueryHandlerTests
    {
        [Fact]
        public async Task WithValidEventName_ReturnsAllPrinters()
        {
            var printers = new List<Printer>
            {
                CreatePrinter("TestEvent", "Printer1"),
                CreatePrinter("TestEvent", "Printer2")
            };
            SetupPrinters("TestEvent", printers);
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            result.Printers.Should().HaveCount(2);
        }

        [Fact]
        public async Task WithValidEventName_MapsStatusCorrectly()
        {
            var onlinePrinter = CreatePrinterWithHeartbeat("TestEvent", "Online", DateTimeOffset.UtcNow);
            var offlinePrinter = CreatePrinterWithHeartbeat("TestEvent", "Offline", DateTimeOffset.UtcNow.AddMinutes(-5));
            var printers = new List<Printer> { onlinePrinter, offlinePrinter };
            SetupPrinters("TestEvent", printers);
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            result.Printers.Should().Contain(p => p.PrinterName == "Online" && p.Status == "Online");
            result.Printers.Should().Contain(p => p.PrinterName == "Offline" && p.Status == "Offline");
        }

        [Fact]
        public async Task WithNoPrinters_ReturnsEmptyList()
        {
            SetupPrinters("TestEvent", new List<Printer>());
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            result.Printers.Should().BeEmpty();
        }

        [Fact]
        public async Task WithEmptyEventName_ThrowsArgumentException()
        {
            var query = new GetPrinterStatusesQuery { EventName = "" };

            var action = async () => await _handler.Handle(query);

            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Event name is required*");
        }

        [Fact]
        public async Task WithNullEventName_ThrowsArgumentException()
        {
            var query = new GetPrinterStatusesQuery { EventName = null! };

            var action = async () => await _handler.Handle(query);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithNullQuery_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithValidEventName_IncludesAllStatusFields()
        {
            var lastHeartbeat = DateTimeOffset.UtcNow;
            var lastJobProcessed = DateTimeOffset.UtcNow.AddMinutes(-1);
            var printer = CreatePrinterWithTimestamps("TestEvent", "Printer1", lastHeartbeat, lastJobProcessed);
            SetupPrinters("TestEvent", new List<Printer> { printer });
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            var status = result.Printers[0];
            status.PrinterId.Should().NotBeNullOrEmpty();
            status.PrinterName.Should().Be("Printer1");
            status.LastHeartbeat.Should().Be(lastHeartbeat);
            status.LastJobProcessed.Should().Be(lastJobProcessed);
        }

        [Fact]
        public async Task WithActiveJobs_ReturnsActiveJobCount()
        {
            var printer = CreatePrinter("TestEvent", "Printer1");
            SetupPrinters("TestEvent", new List<Printer> { printer });
            A.CallTo(() => _printJobRepository.CountActiveJobsForPrinterAsync(printer.Id!.Value))
                .Returns(5);
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            result.Printers[0].ActiveJobCount.Should().Be(5);
        }

        [Fact]
        public async Task WithNoActiveJobs_ReturnsZeroActiveJobCount()
        {
            var printer = CreatePrinter("TestEvent", "Printer1");
            SetupPrinters("TestEvent", new List<Printer> { printer });
            A.CallTo(() => _printJobRepository.CountActiveJobsForPrinterAsync(printer.Id!.Value))
                .Returns(0);
            var query = new GetPrinterStatusesQuery { EventName = "TestEvent" };

            var result = await _handler.Handle(query);

            result.Printers[0].ActiveJobCount.Should().Be(0);
        }

        private void SetupPrinters(string eventName, List<Printer> printers)
        {
            A.CallTo(() => _repository.GetPrintersForEventAsync(eventName))
                .Returns(printers);
        }
    }

    private static Printer CreatePrinter(string eventName, string printerName)
    {
        var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
        return Printer.From(printerId, eventName, printerName, "test-key");
    }

    private static Printer CreatePrinterWithHeartbeat(string eventName, string printerName, DateTimeOffset lastHeartbeat)
    {
        var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
        return Printer.From(printerId, eventName, printerName, "test-key", lastHeartbeat);
    }

    private static Printer CreatePrinterWithTimestamps(string eventName, string printerName, DateTimeOffset lastHeartbeat, DateTimeOffset lastJobProcessed)
    {
        var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
        return Printer.From(printerId, eventName, printerName, "test-key", lastHeartbeat, lastJobProcessed);
    }
}
