/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class PrintJobEntityTests
{
    public class CreateMethod
    {
        [Fact]
        public void WithValidInputs_CreatesQueuedPrintJob()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

            printJob.Status.Should().Be(PrintJobStatus.Queued);
            printJob.PrinterId.Should().Be(printerId);
            printJob.UserId.Should().Be("user123");
            printJob.StickerId.Should().Be("sticker456");
            printJob.StickerUrl.Should().Be("https://example.com/sticker.png");
        }

        [Fact]
        public void WithValidInputs_GeneratesUniqueId()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

            printJob.Id.Should().NotBeNull();
            printJob.Id.Value.Should().NotBeNullOrEmpty();
            Guid.TryParse(printJob.Id.Value, out _).Should().BeTrue();
        }

        [Fact]
        public void WithValidInputs_SetsCreatedAtToNow()
        {
            var before = DateTimeOffset.UtcNow;
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");
            var after = DateTimeOffset.UtcNow;

            printJob.CreatedAt.Should().BeOnOrAfter(before);
            printJob.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void WithValidInputs_RaisesPrintJobQueuedEvent()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

            printJob.DomainEvents.Should().HaveCount(1);
            printJob.DomainEvents.First().Should().BeOfType<PrintJobQueuedEvent>();
        }

        [Fact]
        public void WithValidInputs_QueuedEventContainsCorrectData()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

            var queuedEvent = printJob.DomainEvents.First() as PrintJobQueuedEvent;
            queuedEvent!.PrintJobId.Should().Be(printJob.Id.Value);
            queuedEvent.PrinterId.Should().Be("EVENT-PRINTER");
            queuedEvent.UserId.Should().Be("user123");
            queuedEvent.StickerId.Should().Be("sticker456");
        }

        [Fact]
        public void WithNullPrinterId_ThrowsArgumentNullException()
        {
            var action = () => PrintJob.Create(null!, "user123", "sticker456", "https://example.com/sticker.png");

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithNullUserId_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, null!, "sticker456", "https://example.com/sticker.png");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyUserId_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, "", "sticker456", "https://example.com/sticker.png");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithNullStickerId_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, "user123", null!, "https://example.com/sticker.png");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyStickerId_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, "user123", "", "https://example.com/sticker.png");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithNullStickerUrl_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, "user123", "sticker456", null!);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyStickerUrl_ThrowsArgumentException()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var action = () => PrintJob.Create(printerId, "user123", "sticker456", "");

            action.Should().Throw<ArgumentException>();
        }
    }

    public class MarkAsProcessingMethod
    {
        [Fact]
        public void WhenQueued_TransitionsToProcessing()
        {
            var printJob = CreateQueuedJob();

            printJob.MarkAsProcessing();

            printJob.Status.Should().Be(PrintJobStatus.Processing);
        }

        [Fact]
        public void WhenQueued_SetsProcessedAtTimestamp()
        {
            var printJob = CreateQueuedJob();
            var before = DateTimeOffset.UtcNow;

            printJob.MarkAsProcessing();

            printJob.ProcessedAt.Should().NotBeNull();
            printJob.ProcessedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void WhenAlreadyProcessing_ThrowsInvalidOperationException()
        {
            var printJob = CreateQueuedJob();
            printJob.MarkAsProcessing();

            var action = () => printJob.MarkAsProcessing();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark job as processing when status is Processing");
        }

        [Fact]
        public void WhenCompleted_ThrowsInvalidOperationException()
        {
            var printJob = CreateProcessingJob();
            printJob.Complete();

            var action = () => printJob.MarkAsProcessing();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark job as processing when status is Completed");
        }

        [Fact]
        public void WhenFailed_ThrowsInvalidOperationException()
        {
            var printJob = CreateProcessingJob();
            printJob.Fail("Some error");

            var action = () => printJob.MarkAsProcessing();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark job as processing when status is Failed");
        }
    }

    public class CompleteMethod
    {
        [Fact]
        public void WhenProcessing_TransitionsToCompleted()
        {
            var printJob = CreateProcessingJob();

            printJob.Complete();

            printJob.Status.Should().Be(PrintJobStatus.Completed);
        }

        [Fact]
        public void WhenProcessing_SetsCompletedAtTimestamp()
        {
            var printJob = CreateProcessingJob();
            var before = DateTimeOffset.UtcNow;

            printJob.Complete();

            printJob.CompletedAt.Should().NotBeNull();
            printJob.CompletedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void WhenProcessing_RaisesPrintJobCompletedEvent()
        {
            var printJob = CreateProcessingJob();
            printJob.ClearDomainEvents();

            printJob.Complete();

            printJob.DomainEvents.Should().HaveCount(1);
            printJob.DomainEvents.First().Should().BeOfType<PrintJobCompletedEvent>();
        }

        [Fact]
        public void WhenQueued_ThrowsInvalidOperationException()
        {
            var printJob = CreateQueuedJob();

            var action = () => printJob.Complete();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot complete job when status is Queued");
        }

        [Fact]
        public void WhenAlreadyCompleted_ThrowsInvalidOperationException()
        {
            var printJob = CreateProcessingJob();
            printJob.Complete();

            var action = () => printJob.Complete();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot complete job when status is Completed");
        }
    }

    public class FailMethod
    {
        [Fact]
        public void WhenProcessing_TransitionsToFailed()
        {
            var printJob = CreateProcessingJob();

            printJob.Fail("Printer out of paper");

            printJob.Status.Should().Be(PrintJobStatus.Failed);
        }

        [Fact]
        public void WhenProcessing_SetsCompletedAtTimestamp()
        {
            var printJob = CreateProcessingJob();
            var before = DateTimeOffset.UtcNow;

            printJob.Fail("Printer error");

            printJob.CompletedAt.Should().NotBeNull();
            printJob.CompletedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void WhenProcessing_SetsFailureReason()
        {
            var printJob = CreateProcessingJob();

            printJob.Fail("Printer jammed");

            printJob.FailureReason.Should().Be("Printer jammed");
        }

        [Fact]
        public void WhenProcessing_RaisesPrintJobFailedEvent()
        {
            var printJob = CreateProcessingJob();
            printJob.ClearDomainEvents();

            printJob.Fail("Some error");

            printJob.DomainEvents.Should().HaveCount(1);
            printJob.DomainEvents.First().Should().BeOfType<PrintJobFailedEvent>();
        }

        [Fact]
        public void WhenQueued_ThrowsInvalidOperationException()
        {
            var printJob = CreateQueuedJob();

            var action = () => printJob.Fail("Error");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot fail job when status is Queued");
        }

        [Fact]
        public void WithNullReason_ThrowsArgumentException()
        {
            var printJob = CreateProcessingJob();

            var action = () => printJob.Fail(null!);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyReason_ThrowsArgumentException()
        {
            var printJob = CreateProcessingJob();

            var action = () => printJob.Fail("");

            action.Should().Throw<ArgumentException>();
        }
    }

    public class FromMethod
    {
        [Fact]
        public void WithValidInputs_ReconstructsPrintJobWithoutRaisingEvents()
        {
            var printJobId = new PrintJobId(Guid.NewGuid().ToString());
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.From(
                printJobId,
                printerId,
                "user123",
                "sticker456",
                "https://example.com/sticker.png",
                PrintJobStatus.Queued,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                null,
                null,
                null);

            printJob.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void WithValidInputs_PreservesAllProperties()
        {
            var printJobId = new PrintJobId(Guid.NewGuid().ToString());
            var printerId = new PrinterId("EVENT-PRINTER");
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            var processedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var completedAt = DateTimeOffset.UtcNow;

            var printJob = PrintJob.From(
                printJobId,
                printerId,
                "user123",
                "sticker456",
                "https://example.com/sticker.png",
                PrintJobStatus.Completed,
                createdAt,
                processedAt,
                completedAt,
                null);

            printJob.Id.Should().Be(printJobId);
            printJob.PrinterId.Should().Be(printerId);
            printJob.UserId.Should().Be("user123");
            printJob.StickerId.Should().Be("sticker456");
            printJob.StickerUrl.Should().Be("https://example.com/sticker.png");
            printJob.Status.Should().Be(PrintJobStatus.Completed);
            printJob.CreatedAt.Should().Be(createdAt);
            printJob.ProcessedAt.Should().Be(processedAt);
            printJob.CompletedAt.Should().Be(completedAt);
        }

        [Fact]
        public void WithFailureReason_PreservesFailureReason()
        {
            var printJobId = new PrintJobId(Guid.NewGuid().ToString());
            var printerId = new PrinterId("EVENT-PRINTER");

            var printJob = PrintJob.From(
                printJobId,
                printerId,
                "user123",
                "sticker456",
                "https://example.com/sticker.png",
                PrintJobStatus.Failed,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow,
                "Printer disconnected");

            printJob.FailureReason.Should().Be("Printer disconnected");
        }
    }

    public class PrintJobIdValueObject
    {
        [Fact]
        public void WithNullValue_ThrowsArgumentException()
        {
            var action = () => new PrintJobId(null!);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyValue_ThrowsArgumentException()
        {
            var action = () => new PrintJobId("");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithValidValue_CreatesInstance()
        {
            var id = Guid.NewGuid().ToString();
            var printJobId = new PrintJobId(id);

            printJobId.Value.Should().Be(id);
        }

        [Fact]
        public void NewId_GeneratesUniqueGuid()
        {
            var id1 = PrintJobId.NewId();
            var id2 = PrintJobId.NewId();

            id1.Value.Should().NotBe(id2.Value);
            Guid.TryParse(id1.Value, out _).Should().BeTrue();
        }

        [Fact]
        public void TwoInstancesWithSameValue_AreEqual()
        {
            var value = Guid.NewGuid().ToString();
            var id1 = new PrintJobId(value);
            var id2 = new PrintJobId(value);

            id1.Should().Be(id2);
        }
    }

    private static PrintJob CreateQueuedJob()
    {
        var printerId = new PrinterId("EVENT-PRINTER");
        return PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");
    }

    private static PrintJob CreateProcessingJob()
    {
        var printJob = CreateQueuedJob();
        printJob.MarkAsProcessing();
        return printJob;
    }
}
