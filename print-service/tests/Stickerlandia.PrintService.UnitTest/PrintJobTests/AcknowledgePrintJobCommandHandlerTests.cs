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

public class AcknowledgePrintJobCommandHandlerTests : IDisposable
{
    private readonly IPrintJobRepository _printJobRepository;
    private readonly IPrinterRepository _printerRepository;
    private readonly IOutbox _outbox;
    private readonly PrintJobInstrumentation _instrumentation;
    private readonly AcknowledgePrintJobCommandHandler _handler;

    public AcknowledgePrintJobCommandHandlerTests()
    {
        _printJobRepository = A.Fake<IPrintJobRepository>();
        _printerRepository = A.Fake<IPrinterRepository>();
        _outbox = A.Fake<IOutbox>();
        _instrumentation = new PrintJobInstrumentation();
        _handler = new AcknowledgePrintJobCommandHandler(_printJobRepository, _printerRepository, _outbox, _instrumentation);
    }

    public void Dispose()
    {
        _instrumentation.Dispose();
        GC.SuppressFinalize(this);
    }

    public class HandleMethod : AcknowledgePrintJobCommandHandlerTests
    {
        [Fact]
        public async Task WithValidSuccessCommand_CompletesJob()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            await _handler.Handle(command);

            printJob.Status.Should().Be(PrintJobStatus.Completed);
        }

        [Fact]
        public async Task WithValidSuccessCommand_UpdatesRepository()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            await _handler.Handle(command);

            A.CallTo(() => _printJobRepository.UpdateAsync(A<PrintJob>.That.Matches(j =>
                j.Status == PrintJobStatus.Completed)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidSuccessCommand_StoresCompletedEvent()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            await _handler.Handle(command);

            A.CallTo(() => _outbox.StoreEventFor(A<PrintJobCompletedEvent>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidSuccessCommand_ReturnsAcknowledged()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            var result = await _handler.Handle(command);

            result.Acknowledged.Should().BeTrue();
        }

        [Fact]
        public async Task WithValidFailureCommand_FailsJob()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateFailureCommand(printJob.Id!.Value, "printer-123", "Paper jam");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            await _handler.Handle(command);

            printJob.Status.Should().Be(PrintJobStatus.Failed);
            printJob.FailureReason.Should().Be("Paper jam");
        }

        [Fact]
        public async Task WithValidFailureCommand_StoresFailedEvent()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateFailureCommand(printJob.Id!.Value, "printer-123", "Paper jam");
            SetupPrintJob(printJob);
            SetupPrinter("printer-123");

            await _handler.Handle(command);

            A.CallTo(() => _outbox.StoreEventFor(A<PrintJobFailedEvent>._))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithValidCommand_UpdatesPrinterLastJobProcessed()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);
            var printer = SetupPrinter("printer-123");

            await _handler.Handle(command);

            A.CallTo(() => _printerRepository.UpdateAsync(A<Printer>.That.Matches(p =>
                p.LastJobProcessed.HasValue)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task WithNonExistentJob_ThrowsPrintJobNotFoundException()
        {
            var command = CreateSuccessCommand("non-existent-job", "printer-123");
            A.CallTo(() => _printJobRepository.GetByIdAsync("non-existent-job"))
                .Returns((PrintJob?)null);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrintJobNotFoundException>()
                .WithMessage("Print job 'non-existent-job' not found");
        }

        [Fact]
        public async Task WithWrongPrinter_ThrowsPrintJobOwnershipException()
        {
            var printJob = CreateProcessingPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "different-printer");
            SetupPrintJob(printJob);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrintJobOwnershipException>();
        }

        [Fact]
        public async Task WithQueuedJob_ThrowsPrintJobStatusException()
        {
            var printJob = CreateQueuedPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrintJobStatusException>()
                .WithMessage($"Print job '{printJob.Id!.Value}' is not in Processing status (current: Queued)");
        }

        [Fact]
        public async Task WithCompletedJob_ThrowsPrintJobStatusException()
        {
            var printJob = CreateCompletedPrintJob("printer-123");
            var command = CreateSuccessCommand(printJob.Id!.Value, "printer-123");
            SetupPrintJob(printJob);

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<PrintJobStatusException>()
                .WithMessage($"Print job '{printJob.Id!.Value}' is not in Processing status (current: Completed)");
        }

        [Fact]
        public async Task WithInvalidCommand_ThrowsInvalidPrintJobException()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "",
                Success = true,
                PrinterId = "printer-123"
            };

            var action = async () => await _handler.Handle(command);

            await action.Should().ThrowAsync<InvalidPrintJobException>()
                .WithMessage("Invalid acknowledgment command");
        }

        [Fact]
        public async Task WithNullCommand_ThrowsArgumentNullException()
        {
            var action = async () => await _handler.Handle(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        private void SetupPrintJob(PrintJob printJob)
        {
            A.CallTo(() => _printJobRepository.GetByIdAsync(printJob.Id!.Value))
                .Returns(printJob);
        }

        private Printer SetupPrinter(string printerId)
        {
            var printer = Printer.From(new PrinterId(printerId), "TestEvent", "TestPrinter", "test-key");
            A.CallTo(() => _printerRepository.GetPrinterByKeyAsync(printerId))
                .Returns(printer);
            return printer;
        }
    }

    private static PrintJob CreateProcessingPrintJob(string printerId)
    {
        var printJob = PrintJob.Create(
            new PrinterId(printerId),
            "user123",
            "sticker456",
            "https://example.com/sticker.png");
        printJob.MarkAsProcessing();
        return printJob;
    }

    private static PrintJob CreateQueuedPrintJob(string printerId)
    {
        return PrintJob.Create(
            new PrinterId(printerId),
            "user123",
            "sticker456",
            "https://example.com/sticker.png");
    }

    private static PrintJob CreateCompletedPrintJob(string printerId)
    {
        var printJob = PrintJob.Create(
            new PrinterId(printerId),
            "user123",
            "sticker456",
            "https://example.com/sticker.png");
        printJob.MarkAsProcessing();
        printJob.Complete();
        return printJob;
    }

    private static AcknowledgePrintJobCommand CreateSuccessCommand(string printJobId, string printerId)
    {
        return new AcknowledgePrintJobCommand
        {
            PrintJobId = printJobId,
            Success = true,
            PrinterId = printerId
        };
    }

    private static AcknowledgePrintJobCommand CreateFailureCommand(string printJobId, string printerId, string reason)
    {
        return new AcknowledgePrintJobCommand
        {
            PrintJobId = printJobId,
            Success = false,
            FailureReason = reason,
            PrinterId = printerId
        };
    }
}
