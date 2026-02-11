/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class RegisterPrinterCommandTests
{
    public class IsValidMethod
    {
        [Fact]
        public void WithBothFieldsPopulated_ReturnsTrue()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = "TestPrinter"
            };

            command.IsValid().Should().BeTrue();
        }

        [Fact]
        public void WithEmptyEventName_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "",
                PrinterName = "TestPrinter"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithEmptyPrinterName_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = ""
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithBothFieldsEmpty_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "",
                PrinterName = ""
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithDefaultValues_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand();

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithNullEventName_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = null!,
                PrinterName = "TestPrinter"
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithNullPrinterName_ReturnsFalse()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "TestEvent",
                PrinterName = null!
            };

            command.IsValid().Should().BeFalse();
        }

        [Fact]
        public void WithWhitespaceEventName_ReturnsTrue()
        {
            var command = new RegisterPrinterCommand
            {
                EventName = "   ",
                PrinterName = "TestPrinter"
            };

            // Note: IsValid uses string.IsNullOrEmpty, not IsNullOrWhiteSpace
            command.IsValid().Should().BeTrue();
        }
    }
}
