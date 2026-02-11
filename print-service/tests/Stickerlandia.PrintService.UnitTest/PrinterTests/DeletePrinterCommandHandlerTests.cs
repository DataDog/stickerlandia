// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class DeletePrinterCommandHandlerTests
{
    private readonly IOutbox _outbox;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly DeletePrinterCommandHandler _handler;

    public DeletePrinterCommandHandlerTests()
    {
        _outbox = A.Fake<IOutbox>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _handler = new DeletePrinterCommandHandler(_outbox, _printerRepository, _printJobRepository);
    }

    public class HandleMethod : DeletePrinterCommandHandlerTests
    {
        [Fact]
        public async Task WithExistingPrinter_DeletesPrinterAndJobs()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns(printer);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer.Id!.Value, PrintJobStatus.Processing))
                .Returns(false);

            await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter"));

            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(printer.Id!.Value))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printerRepository.DeleteAsync("TestEvent", "TestPrinter"))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithExistingPrinter_StoresDeletedEventInOutbox()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns(printer);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer.Id!.Value, PrintJobStatus.Processing))
                .Returns(false);

            await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter"));

            A.CallTo(() => _outbox.StoreEventFor(A<PrinterDeletedEvent>.That.Matches(e =>
                e.PrinterId == "TESTEVENT-TESTPRINTER")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNonExistentPrinter_ThrowsPrinterNotFoundException()
        {
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns((Printer?)null);

            var action = async () => await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter"));

            await action.Should().ThrowAsync<PrinterNotFoundException>();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsFalse_ThrowsPrinterHasActiveJobsException()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns(printer);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer.Id!.Value, PrintJobStatus.Processing))
                .Returns(true);

            var action = async () => await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter", Force: false));

            await action.Should().ThrowAsync<PrinterHasActiveJobsException>();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsTrue_DeletesAnyway()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns(printer);

            await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter", Force: true));

            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(A<string>._, A<PrintJobStatus>._))
                .MustNotHaveHappened();
            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(printer.Id!.Value))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printerRepository.DeleteAsync("TestEvent", "TestPrinter"))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsFalse_DoesNotDeleteAnything()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            A.CallTo(() => _printerRepository.GetPrinterAsync("TestEvent", "TestPrinter"))
                .Returns(printer);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer.Id!.Value, PrintJobStatus.Processing))
                .Returns(true);

            try
            {
                await _handler.Handle(new DeletePrinterCommand("TestEvent", "TestPrinter", Force: false));
            }
            catch (PrinterHasActiveJobsException)
            {
                // Expected
            }

            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(A<string>._))
                .MustNotHaveHappened();
            A.CallTo(() => _printerRepository.DeleteAsync(A<string>._, A<string>._))
                .MustNotHaveHappened();
            A.CallTo(() => _outbox.StoreEventFor(A<DomainEvent>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
