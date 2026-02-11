/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class PrinterDTOTests
{
    public class Constructor
    {
        [Fact]
        public void WithValidPrinter_MapsAllProperties()
        {
            var printer = Printer.From(
                new PrinterId("MYEVENT-MYPRINTER"),
                "MyEvent",
                "MyPrinter",
                "secretKey123");

            var dto = new PrinterDTO(printer);

            dto.PrinterId.Should().Be("MYEVENT-MYPRINTER");
            dto.EventName.Should().Be("MyEvent");
            dto.PrinterName.Should().Be("MyPrinter");
        }

        [Fact]
        public void WithNullPrinter_ThrowsArgumentNullException()
        {
            var action = () => new PrinterDTO(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithRegisteredPrinter_MapsPropertiesCorrectly()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            var dto = new PrinterDTO(printer);

            dto.PrinterId.Should().Be("TESTEVENT-TESTPRINTER");
            dto.EventName.Should().Be("TestEvent");
            dto.PrinterName.Should().Be("TestPrinter");
        }
    }
}

public class RegisterPrinterResponseTests
{
    public class Constructor
    {
        [Fact]
        public void WithValidPrinter_IncludesKey()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            var response = new RegisterPrinterResponse(printer);

            response.Key.Should().NotBeNullOrEmpty();
            response.Key.Should().Be(printer.Key);
        }

        [Fact]
        public void WithValidPrinter_MapsBaseProperties()
        {
            var printer = Printer.Register("MyEvent", "MyPrinter");

            var response = new RegisterPrinterResponse(printer);

            response.PrinterId.Should().Be("MYEVENT-MYPRINTER");
            response.EventName.Should().Be("MyEvent");
            response.PrinterName.Should().Be("MyPrinter");
        }

        [Fact]
        public void WithReconstructedPrinter_PreservesKey()
        {
            var printer = Printer.From(
                new PrinterId("TEST-PRINTER"),
                "Test",
                "Printer",
                "preserved-key-123");

            var response = new RegisterPrinterResponse(printer);

            response.Key.Should().Be("preserved-key-123");
        }
    }
}
