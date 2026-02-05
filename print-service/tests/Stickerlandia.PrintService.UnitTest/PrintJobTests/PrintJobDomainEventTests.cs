/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class PrintJobDomainEventTests
{
    public class PrintJobQueuedEventTests
    {
        [Fact]
        public void EventName_ReturnsPrintJobsQueuedV1()
        {
            var evt = new PrintJobQueuedEvent();

            evt.EventName.Should().Be("printJobs.queued.v1");
        }

        [Fact]
        public void EventVersion_Returns1Point0()
        {
            var evt = new PrintJobQueuedEvent();

            evt.EventVersion.Should().Be("1.0");
        }

        [Fact]
        public void Constructor_WithPrintJob_PopulatesAllFields()
        {
            var printerId = new PrinterId("EVENT-PRINTER");
            var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");

            var evt = new PrintJobQueuedEvent(printJob);

            evt.PrintJobId.Should().Be(printJob.Id.Value);
            evt.PrinterId.Should().Be("EVENT-PRINTER");
            evt.UserId.Should().Be("user123");
            evt.StickerId.Should().Be("sticker456");
        }

        [Fact]
        public void ToJsonString_ProducesValidJson()
        {
            var evt = new PrintJobQueuedEvent
            {
                PrintJobId = "job-123",
                PrinterId = "EVENT-PRINTER",
                UserId = "user456",
                StickerId = "sticker789"
            };

            var json = evt.ToJsonString();

            var deserialized = JsonSerializer.Deserialize<PrintJobQueuedEvent>(json);
            deserialized.Should().NotBeNull();
            deserialized!.PrintJobId.Should().Be("job-123");
            deserialized.PrinterId.Should().Be("EVENT-PRINTER");
            deserialized.UserId.Should().Be("user456");
            deserialized.StickerId.Should().Be("sticker789");
        }

        [Fact]
        public void Constructor_WithNullPrintJob_ThrowsArgumentNullException()
        {
            var action = () => new PrintJobQueuedEvent(null!);

            action.Should().Throw<ArgumentNullException>();
        }
    }

    public class PrintJobCompletedEventTests
    {
        [Fact]
        public void EventName_ReturnsPrintJobsCompletedV1()
        {
            var evt = new PrintJobCompletedEvent();

            evt.EventName.Should().Be("printJobs.completed.v1");
        }

        [Fact]
        public void EventVersion_Returns1Point0()
        {
            var evt = new PrintJobCompletedEvent();

            evt.EventVersion.Should().Be("1.0");
        }

        [Fact]
        public void Constructor_WithPrintJob_PopulatesAllFields()
        {
            var printJob = CreateCompletedPrintJob();

            var evt = new PrintJobCompletedEvent(printJob);

            evt.PrintJobId.Should().Be(printJob.Id.Value);
            evt.PrinterId.Should().Be(printJob.PrinterId.Value);
            evt.CompletedAt.Should().Be(printJob.CompletedAt!.Value);
        }

        [Fact]
        public void ToJsonString_ProducesValidJson()
        {
            var completedAt = DateTimeOffset.UtcNow;
            var evt = new PrintJobCompletedEvent
            {
                PrintJobId = "job-123",
                PrinterId = "EVENT-PRINTER",
                CompletedAt = completedAt
            };

            var json = evt.ToJsonString();

            var deserialized = JsonSerializer.Deserialize<PrintJobCompletedEvent>(json);
            deserialized.Should().NotBeNull();
            deserialized!.PrintJobId.Should().Be("job-123");
            deserialized.PrinterId.Should().Be("EVENT-PRINTER");
        }

        [Fact]
        public void Constructor_WithNullPrintJob_ThrowsArgumentNullException()
        {
            var action = () => new PrintJobCompletedEvent(null!);

            action.Should().Throw<ArgumentNullException>();
        }
    }

    public class PrintJobFailedEventTests
    {
        [Fact]
        public void EventName_ReturnsPrintJobsFailedV1()
        {
            var evt = new PrintJobFailedEvent();

            evt.EventName.Should().Be("printJobs.failed.v1");
        }

        [Fact]
        public void EventVersion_Returns1Point0()
        {
            var evt = new PrintJobFailedEvent();

            evt.EventVersion.Should().Be("1.0");
        }

        [Fact]
        public void Constructor_WithPrintJob_PopulatesAllFields()
        {
            var printJob = CreateFailedPrintJob("Printer disconnected");

            var evt = new PrintJobFailedEvent(printJob);

            evt.PrintJobId.Should().Be(printJob.Id.Value);
            evt.PrinterId.Should().Be(printJob.PrinterId.Value);
            evt.FailureReason.Should().Be("Printer disconnected");
        }

        [Fact]
        public void ToJsonString_ProducesValidJson()
        {
            var evt = new PrintJobFailedEvent
            {
                PrintJobId = "job-123",
                PrinterId = "EVENT-PRINTER",
                FailureReason = "Out of paper"
            };

            var json = evt.ToJsonString();

            var deserialized = JsonSerializer.Deserialize<PrintJobFailedEvent>(json);
            deserialized.Should().NotBeNull();
            deserialized!.PrintJobId.Should().Be("job-123");
            deserialized.PrinterId.Should().Be("EVENT-PRINTER");
            deserialized.FailureReason.Should().Be("Out of paper");
        }

        [Fact]
        public void Constructor_WithNullPrintJob_ThrowsArgumentNullException()
        {
            var action = () => new PrintJobFailedEvent(null!);

            action.Should().Throw<ArgumentNullException>();
        }
    }

    private static PrintJob CreateCompletedPrintJob()
    {
        var printerId = new PrinterId("EVENT-PRINTER");
        var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");
        printJob.MarkAsProcessing();
        printJob.Complete();
        return printJob;
    }

    private static PrintJob CreateFailedPrintJob(string reason)
    {
        var printerId = new PrinterId("EVENT-PRINTER");
        var printJob = PrintJob.Create(printerId, "user123", "sticker456", "https://example.com/sticker.png");
        printJob.MarkAsProcessing();
        printJob.Fail(reason);
        return printJob;
    }
}
