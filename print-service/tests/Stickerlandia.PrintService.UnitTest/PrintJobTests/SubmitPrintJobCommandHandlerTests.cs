/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

#pragma warning disable CA1063 // Implement IDisposable correctly - test class

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class SubmitPrintJobCommandHandlerTests : IDisposable
{
    private readonly IOutbox _outbox;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly PrintJobInstrumentation _instrumentation;
    private readonly SubmitPrintJobCommandHandler _handler;

    public SubmitPrintJobCommandHandlerTests()
    {
        _outbox = A.Fake<IOutbox>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _instrumentation = new PrintJobInstrumentation();
        _handler = new SubmitPrintJobCommandHandler(_outbox, _printerRepository, _printJobRepository, _instrumentation);
    }

    public void Dispose()
    {
        _instrumentation.Dispose();
        GC.SuppressFinalize(this);
    }

    public class HandleMethod : SubmitPrintJobCommandHandlerTests
    {
        [Fact]
        public async Task WithValidCommand_CreatesPrintJob()
        {
            var command = CreateValidCommand("TestEvent", "TestPrinter");
            SetupExistingPrinter(command.EventName, command.PrinterName);

            await _handler.Handle(command);

            A.CallTo(() => _printJobRepository.AddAsync(A<PrintJob>.That.Matches(j =>
                j.UserId == command.UserId &&
                j.StickerId == command.StickerId &&
                j.StickerUrl == command.StickerUrl &&
                j.Status == PrintJobStatus.Queued)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_StoresEventInOutbox()
        {
            var command = CreateValidCommand("TestEvent", "TestPrinter");
            SetupExistingPrinter(command.EventName, command.PrinterName);

            await _handler.Handle(command);

            A.CallTo(() => _outbox.StoreEventFor(A<PrintJobQueuedEvent>.That.Matches(e =>
                e.UserId == command.UserId &&
                e.StickerId == command.StickerId)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_ReturnsResponseWithPrintJobId()
        {
            var command = CreateValidCommand("TestEvent", "TestPrinter");
            SetupExistingPrinter(command.EventName, command.PrinterName);

            var result = await _handler.Handle(command);

            result.Should().NotBeNull();
            result.PrintJobId.Should().NotBeNullOrEmpty();
            Guid.TryParse(result.PrintJobId, out _).Should().BeTrue();
        }

        [Fact]
        public async Task WithValidCommand_LinksPrintJobToPrinter()
        {
            var command = CreateValidCommand("TestEvent", "TestPrinter");
            SetupExistingPrinter(command.EventName, command.PrinterName);

            await _handler.Handle(command);

            A.CallTo(() => _printJobRepository.AddAsync(A<PrintJob>.That.Matches(j =>
                j.PrinterId.Value == "TESTEVENT-TESTPRINTER")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNonExistentPrinter_ThrowsPrinterNotFoundException()
        {
            var command = CreateValidCommand("TestEvent", "NonExistentPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync(command.EventName, command.PrinterName))
                .Returns((Printer?)null);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrinterNotFoundException>()
                .WithMessage($"Printer '{command.PrinterName}' not found for event '{command.EventName}'.");
        }

        [Fact]
        public async Task WithNonExistentPrinter_DoesNotAddPrintJob()
        {
            var command = CreateValidCommand("TestEvent", "NonExistentPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync(command.EventName, command.PrinterName))
                .Returns((Printer?)null);

            try
            {
                await _handler.Handle(command);
            }
            catch (PrinterNotFoundException)
            {
                // Expected
            }

            A.CallTo(() => _printJobRepository.AddAsync(A<PrintJob>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WithNonExistentPrinter_DoesNotStoreInOutbox()
        {
            var command = CreateValidCommand("TestEvent", "NonExistentPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync(command.EventName, command.PrinterName))
                .Returns((Printer?)null);

            try
            {
                await _handler.Handle(command);
            }
            catch (PrinterNotFoundException)
            {
                // Expected
            }

            A.CallTo(() => _outbox.StoreEventFor(A<DomainEvent>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WithNullEventName_ThrowsArgumentException()
        {
            var command = CreateValidCommand(null!, "TestPrinter");

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithEmptyEventName_ThrowsArgumentException()
        {
            var command = CreateValidCommand("", "TestPrinter");

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithNullPrinterName_ThrowsArgumentException()
        {
            var command = CreateValidCommand("TestEvent", null!);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithEmptyPrinterName_ThrowsArgumentException()
        {
            var command = CreateValidCommand("TestEvent", "");

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingUserId_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter",
                UserId = "",
                StickerId = "sticker123",
                StickerUrl = "https://example.com/sticker.png"
            };

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingStickerId_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter",
                UserId = "user123",
                StickerId = "",
                StickerUrl = "https://example.com/sticker.png"
            };

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingStickerUrl_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter",
                UserId = "user123",
                StickerId = "sticker123",
                StickerUrl = ""
            };

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        private void SetupExistingPrinter(string eventName, string printerName)
        {
            var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
            var printer = Printer.From(printerId, eventName, printerName, "test-key");

            A.CallTo(() => _printerRepository.GetPrinterAsync(eventName, printerName))
                .Returns(printer);
        }
    }

    private static SubmitPrintJobCommand CreateValidCommand(string eventName = "TestEvent", string printerName = "TestPrinter")
    {
        return new SubmitPrintJobCommand
        {
            EventName = eventName,
            PrinterName = printerName,
            UserId = "user123",
            StickerId = "sticker456",
            StickerUrl = "https://example.com/stickers/456.png"
        };
    }
}
