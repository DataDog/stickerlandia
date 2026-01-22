/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.UnitTest.PrintJobTests;

public class AcknowledgePrintJobCommandTests
{
    public class IsValidMethod
    {
        [Fact]
        public void WithValidSuccessCommand_ReturnsTrue()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "job-123",
                Success = true,
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeTrue();
        }

        [Fact]
        public void WithValidFailureCommand_ReturnsTrue()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "job-123",
                Success = false,
                FailureReason = "Paper jam",
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeTrue();
        }

        [Fact]
        public void WithEmptyPrintJobId_ReturnsFalse()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "",
                Success = true,
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeFalse();
        }

        [Fact]
        public void WithNullPrintJobId_ReturnsFalse()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = null!,
                Success = true,
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeFalse();
        }

        [Fact]
        public void WithFailureAndNoReason_ReturnsFalse()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "job-123",
                Success = false,
                FailureReason = null,
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeFalse();
        }

        [Fact]
        public void WithFailureAndEmptyReason_ReturnsFalse()
        {
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "job-123",
                Success = false,
                FailureReason = "",
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeFalse();
        }

        [Fact]
        public void WithSuccessAndFailureReason_ReturnsTrue()
        {
            // Even if there's a failure reason, if success is true it should be valid
            var command = new AcknowledgePrintJobCommand
            {
                PrintJobId = "job-123",
                Success = true,
                FailureReason = "This should be ignored",
                PrinterId = "printer-123"
            };

            var result = command.IsValid();

            result.Should().BeTrue();
        }
    }
}
