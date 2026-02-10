/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class PrinterEntityTests
{
    public class RegisterMethod
    {
        [Fact]
        public void WithValidInputs_CreatesPrinterWithCorrectIdFormat()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            printer.Id.Should().NotBeNull();
            printer.Id!.Value.Should().Be("TESTEVENT-TESTPRINTER");
        }

        [Fact]
        public void WithValidInputs_GeneratesBase64Key()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            printer.Key.Should().NotBeNullOrEmpty();
            var action = () => Convert.FromBase64String(printer.Key);
            action.Should().NotThrow();
        }

        [Fact]
        public void WithValidInputs_RaisesPrinterRegisteredEvent()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            printer.DomainEvents.Should().HaveCount(1);
            printer.DomainEvents.First().Should().BeOfType<PrinterRegisteredEvent>();
        }

        [Fact]
        public void WithValidInputs_SetsEventAndPrinterNames()
        {
            var printer = Printer.Register("MyEvent", "MyPrinter");

            printer.EventName.Should().Be("MyEvent");
            printer.PrinterName.Should().Be("MyPrinter");
        }

        [Fact]
        public void WithNullEventName_ThrowsArgumentNullException()
        {
            var action = () => Printer.Register(null!, "TestPrinter");

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithEmptyEventName_ThrowsArgumentException()
        {
            var action = () => Printer.Register("", "TestPrinter");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithNullPrinterName_ThrowsArgumentNullException()
        {
            var action = () => Printer.Register("TestEvent", null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithEmptyPrinterName_ThrowsArgumentException()
        {
            var action = () => Printer.Register("TestEvent", "");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithLowercaseInputs_ConvertsIdToUppercase()
        {
            var printer = Printer.Register("lowercase", "printer");

            printer.Id!.Value.Should().Be("LOWERCASE-PRINTER");
        }

        [Fact]
        public void WithMixedCaseInputs_ConvertsIdToUppercase()
        {
            var printer = Printer.Register("MixedCase", "PrinterName");

            printer.Id!.Value.Should().Be("MIXEDCASE-PRINTERNAME");
        }
    }

    public class FromMethod
    {
        [Fact]
        public void WithValidInputs_ReconstructsPrinterWithoutRaisingEvents()
        {
            var id = new PrinterId("TEST-PRINTER");
            var printer = Printer.From(id, "Test", "Printer", "abc123key");

            printer.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void WithValidInputs_PreservesAllProperties()
        {
            var id = new PrinterId("MYEVENT-MYPRINTER");
            var printer = Printer.From(id, "MyEvent", "MyPrinter", "secretKey123");

            printer.Id.Should().Be(id);
            printer.EventName.Should().Be("MyEvent");
            printer.PrinterName.Should().Be("MyPrinter");
            printer.Key.Should().Be("secretKey123");
        }
    }

    public class PrinterIdValueObject
    {
        [Fact]
        public void WithNullValue_ThrowsArgumentException()
        {
            var action = () => new PrinterId(null!);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithEmptyValue_ThrowsArgumentException()
        {
            var action = () => new PrinterId("");

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithValidValue_CreatesInstance()
        {
            var printerId = new PrinterId("VALID-ID");

            printerId.Value.Should().Be("VALID-ID");
        }

        [Fact]
        public void TwoInstancesWithSameValue_AreEqual()
        {
            var id1 = new PrinterId("SAME-VALUE");
            var id2 = new PrinterId("SAME-VALUE");

            id1.Should().Be(id2);
        }
    }
}
