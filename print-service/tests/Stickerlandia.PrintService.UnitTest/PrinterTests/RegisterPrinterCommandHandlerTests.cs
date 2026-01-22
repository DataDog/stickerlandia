/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class RegisterPrinterCommandHandlerTests
{
    private readonly IOutbox _outbox;
    private readonly IPrinterRepository _repository;
    private readonly RegisterPrinterCommandHandler _handler;

    public RegisterPrinterCommandHandlerTests()
    {
        _outbox = A.Fake<IOutbox>();
        _repository = A.Fake<IPrinterRepository>();
        _handler = new RegisterPrinterCommandHandler(_outbox, _repository);
    }

    public class HandleMethod : RegisterPrinterCommandHandlerTests
    {
        [Fact]
        public async Task WithValidCommand_RegistersNewPrinter()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(false);

            var result = await _handler.Handle(command);

            A.CallTo(() => _repository.AddPrinterAsync(A<Printer>.That.Matches(p =>
                p.EventName == "TestEvent" &&
                p.PrinterName == "TestPrinter")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_StoresEventInOutbox()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(false);

            await _handler.Handle(command);

            A.CallTo(() => _outbox.StoreEventFor(A<PrinterRegisteredEvent>.That.Matches(e =>
                e.PrinterId == "TESTEVENT-TESTPRINTER")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_ReturnsResponseWithKey()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(false);

            var result = await _handler.Handle(command);

            result.Should().NotBeNull();
            result.Key.Should().NotBeNullOrEmpty();
            result.PrinterId.Should().Be("TESTEVENT-TESTPRINTER");
            result.EventName.Should().Be("TestEvent");
            result.PrinterName.Should().Be("TestPrinter");
        }

        [Fact]
        public async Task WithDuplicatePrinter_ThrowsPrinterExistsException()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "ExistingPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(true);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrinterExistsException>()
                .WithMessage("A printer exists with this name for this event.");
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WithSamePrinterNameDifferentEvent_Succeeds()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "DifferentEvent",
                PrinterName = "SharedPrinterName"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(false);

            var result = await _handler.Handle(command);

            result.Should().NotBeNull();
            result.PrinterId.Should().Be("DIFFERENTEVENT-SHAREDPRINTERNAME");
        }

        [Fact]
        public async Task WithDuplicatePrinter_DoesNotAddToRepository()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "ExistingPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(true);

            try
            {
                await _handler.Handle(command);
            }
            catch (PrinterExistsException)
            {
                // Expected
            }

            A.CallTo(() => _repository.AddPrinterAsync(A<Printer>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WithDuplicatePrinter_DoesNotStoreInOutbox()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "ExistingPrinter"
            };
            A.CallTo(() => _repository.PrinterExistsAsync(command.EventName, command.PrinterName))
                .Returns(true);

            try
            {
                await _handler.Handle(command);
            }
            catch (PrinterExistsException)
            {
                // Expected
            }

            A.CallTo(() => _outbox.StoreEventFor(A<DomainEvent>._))
                .MustNotHaveHappened();
        }
    }
}
