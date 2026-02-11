// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class DeleteEventCommandHandlerTests
{
    private readonly IOutbox _outbox;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrintJobRepository _printJobRepository;
    private readonly DeleteEventCommandHandler _handler;

    public DeleteEventCommandHandlerTests()
    {
        _outbox = A.Fake<IOutbox>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _handler = new DeleteEventCommandHandler(_outbox, _printerRepository, _printJobRepository);
    }

    public class HandleMethod : DeleteEventCommandHandlerTests
    {
        [Fact]
        public async Task WithExistingEvent_DeletesAllPrintersAndJobs()
        {
            var printer1 = Printer.Register("TestEvent", "Printer1");
            var printer2 = Printer.Register("TestEvent", "Printer2");
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("TestEvent"))
                .Returns(new List<Printer> { printer1, printer2 });
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(A<string>._, PrintJobStatus.Processing))
                .Returns(false);

            var result = await _handler.Handle(new DeleteEventCommand("TestEvent"));

            result.PrintersDeleted.Should().Be(2);

            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(printer1.Id!.Value))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(printer2.Id!.Value))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printerRepository.DeleteAsync("TestEvent", "Printer1"))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printerRepository.DeleteAsync("TestEvent", "Printer2"))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithExistingEvent_StoresDeletedEventForEachPrinter()
        {
            var printer1 = Printer.Register("TestEvent", "Printer1");
            var printer2 = Printer.Register("TestEvent", "Printer2");
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("TestEvent"))
                .Returns(new List<Printer> { printer1, printer2 });
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(A<string>._, PrintJobStatus.Processing))
                .Returns(false);

            await _handler.Handle(new DeleteEventCommand("TestEvent"));

            A.CallTo(() => _outbox.StoreEventFor(A<PrinterDeletedEvent>._))
                .MustHaveHappened(2, Times.Exactly);
        }

        [Fact]
        public async Task WithNoPrinters_ThrowsKeyNotFoundException()
        {
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("EmptyEvent"))
                .Returns(new List<Printer>());

            var action = async () => await _handler.Handle(new DeleteEventCommand("EmptyEvent"));

            await action.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsFalse_ThrowsPrinterHasActiveJobsException()
        {
            var printer1 = Printer.Register("TestEvent", "Printer1");
            var printer2 = Printer.Register("TestEvent", "Printer2");
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("TestEvent"))
                .Returns(new List<Printer> { printer1, printer2 });
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer1.Id!.Value, PrintJobStatus.Processing))
                .Returns(false);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer2.Id!.Value, PrintJobStatus.Processing))
                .Returns(true);

            var action = async () => await _handler.Handle(new DeleteEventCommand("TestEvent", Force: false));

            await action.Should().ThrowAsync<PrinterHasActiveJobsException>();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsFalse_DoesNotDeleteAnything()
        {
            var printer1 = Printer.Register("TestEvent", "Printer1");
            var printer2 = Printer.Register("TestEvent", "Printer2");
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("TestEvent"))
                .Returns(new List<Printer> { printer1, printer2 });
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer1.Id!.Value, PrintJobStatus.Processing))
                .Returns(false);
            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(printer2.Id!.Value, PrintJobStatus.Processing))
                .Returns(true);

            try
            {
                await _handler.Handle(new DeleteEventCommand("TestEvent", Force: false));
            }
            catch (PrinterHasActiveJobsException)
            {
                // Expected
            }

            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(A<string>._))
                .MustNotHaveHappened();
            A.CallTo(() => _printerRepository.DeleteAsync(A<string>._, A<string>._))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task WithProcessingJobs_AndForceIsTrue_DeletesAnyway()
        {
            var printer1 = Printer.Register("TestEvent", "Printer1");
            A.CallTo(() => _printerRepository.GetPrintersForEventAsync("TestEvent"))
                .Returns(new List<Printer> { printer1 });

            var result = await _handler.Handle(new DeleteEventCommand("TestEvent", Force: true));

            result.PrintersDeleted.Should().Be(1);

            A.CallTo(() => _printJobRepository.HasJobsInStatusAsync(A<string>._, A<PrintJobStatus>._))
                .MustNotHaveHappened();
            A.CallTo(() => _printJobRepository.DeleteJobsForPrinterAsync(printer1.Id!.Value))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _printerRepository.DeleteAsync("TestEvent", "Printer1"))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
