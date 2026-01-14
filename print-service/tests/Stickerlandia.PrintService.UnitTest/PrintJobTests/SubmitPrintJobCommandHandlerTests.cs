/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class SubmitPrintJobCommandHandlerTests
{
    private readonly IOutbox _outbox;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly SubmitPrintJobCommandHandler _handler;

    public SubmitPrintJobCommandHandlerTests()
    {
        _outbox = A.Fake<IOutbox>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _handler = new SubmitPrintJobCommandHandler(_outbox, _printerRepository, _printJobRepository);
    }

    public class HandleMethod : SubmitPrintJobCommandHandlerTests
    {
        [Fact]
        public async Task WithValidCommand_CreatesPrintJob()
        {
            var eventName = "TestEvent";
            var printerName = "TestPrinter";
            var command = CreateValidCommand();
            SetupExistingPrinter(eventName, printerName);

            await _handler.Handle(eventName, printerName, command);

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
            var eventName = "TestEvent";
            var printerName = "TestPrinter";
            var command = CreateValidCommand();
            SetupExistingPrinter(eventName, printerName);

            await _handler.Handle(eventName, printerName, command);

            A.CallTo(() => _outbox.StoreEventFor(A<PrintJobQueuedEvent>.That.Matches(e =>
                e.UserId == command.UserId &&
                e.StickerId == command.StickerId)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_ReturnsResponseWithPrintJobId()
        {
            var eventName = "TestEvent";
            var printerName = "TestPrinter";
            var command = CreateValidCommand();
            SetupExistingPrinter(eventName, printerName);

            var result = await _handler.Handle(eventName, printerName, command);

            result.Should().NotBeNull();
            result.PrintJobId.Should().NotBeNullOrEmpty();
            Guid.TryParse(result.PrintJobId, out _).Should().BeTrue();
        }

        [Fact]
        public async Task WithValidCommand_LinksPrintJobToPrinter()
        {
            var eventName = "TestEvent";
            var printerName = "TestPrinter";
            var command = CreateValidCommand();
            SetupExistingPrinter(eventName, printerName);

            await _handler.Handle(eventName, printerName, command);

            A.CallTo(() => _printJobRepository.AddAsync(A<PrintJob>.That.Matches(j =>
                j.PrinterId.Value == "TESTEVENT-TESTPRINTER")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNonExistentPrinter_ThrowsPrinterNotFoundException()
        {
            var eventName = "TestEvent";
            var printerName = "NonExistentPrinter";
            var command = CreateValidCommand();
            A.CallTo(() => _printerRepository.GetPrinterAsync(eventName, printerName))
                .Returns((Printer?)null);

            var action = async () => await _handler.Handle(eventName, printerName, command);

            await action.Should().ThrowAsync<PrinterNotFoundException>()
                .WithMessage($"Printer '{printerName}' not found for event '{eventName}'.");
        }

        [Fact]
        public async Task WithNonExistentPrinter_DoesNotAddPrintJob()
        {
            var eventName = "TestEvent";
            var printerName = "NonExistentPrinter";
            var command = CreateValidCommand();
            A.CallTo(() => _printerRepository.GetPrinterAsync(eventName, printerName))
                .Returns((Printer?)null);

            try
            {
                await _handler.Handle(eventName, printerName, command);
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
            var eventName = "TestEvent";
            var printerName = "NonExistentPrinter";
            var command = CreateValidCommand();
            A.CallTo(() => _printerRepository.GetPrinterAsync(eventName, printerName))
                .Returns((Printer?)null);

            try
            {
                await _handler.Handle(eventName, printerName, command);
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
            var command = CreateValidCommand();

            var action = async () => await _handler.Handle(null!, "TestPrinter", command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithEmptyEventName_ThrowsArgumentException()
        {
            var command = CreateValidCommand();

            var action = async () => await _handler.Handle("", "TestPrinter", command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithNullPrinterName_ThrowsArgumentException()
        {
            var command = CreateValidCommand();

            var action = async () => await _handler.Handle("TestEvent", null!, command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithEmptyPrinterName_ThrowsArgumentException()
        {
            var command = CreateValidCommand();

            var action = async () => await _handler.Handle("TestEvent", "", command);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle("TestEvent", "TestPrinter", null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingUserId_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "",
                StickerId = "sticker123",
                StickerUrl = "https://example.com/sticker.png"
            };

            var action = async () => await _handler.Handle("TestEvent", "TestPrinter", command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingStickerId_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "",
                StickerUrl = "https://example.com/sticker.png"
            };

            var action = async () => await _handler.Handle("TestEvent", "TestPrinter", command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        [Fact]
        public async Task WithInvalidCommand_MissingStickerUrl_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker123",
                StickerUrl = ""
            };

            var action = async () => await _handler.Handle("TestEvent", "TestPrinter", command);

            await action.Should().ThrowAsync<InvalidPrintJobException>();
        }

        [Fact]
        public async Task WithInvalidCommand_InvalidStickerUrl_ThrowsInvalidPrintJobException()
        {
            var command = new SubmitPrintJobCommand
            {
                UserId = "user123",
                StickerId = "sticker123",
                StickerUrl = "not-a-valid-url"
            };

            var action = async () => await _handler.Handle("TestEvent", "TestPrinter", command);

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

    private static SubmitPrintJobCommand CreateValidCommand()
    {
        return new SubmitPrintJobCommand
        {
            UserId = "user123",
            StickerId = "sticker456",
            StickerUrl = "https://example.com/stickers/456.png"
        };
    }
}
