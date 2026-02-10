/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Text.Json;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class PrinterRegisteredEventTests
{
    public class ConstructorWithPrinter
    {
        [Fact]
        public void WithValidPrinter_SetsPrinterId()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");

            var domainEvent = new PrinterRegisteredEvent(printer);

            domainEvent.PrinterId.Should().Be("TESTEVENT-TESTPRINTER");
        }

        [Fact]
        public void WithNullPrinter_ThrowsArgumentNullException()
        {
            var action = () => new PrinterRegisteredEvent(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithReconstructedPrinter_ExtractsPrinterId()
        {
            var printer = Printer.From(
                new PrinterId("CUSTOM-ID"),
                "Custom",
                "Id",
                "key");

            var domainEvent = new PrinterRegisteredEvent(printer);

            domainEvent.PrinterId.Should().Be("CUSTOM-ID");
        }
    }

    public class EventNameProperty
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var domainEvent = new PrinterRegisteredEvent();

            domainEvent.EventName.Should().Be("printers.registered.v1");
        }
    }

    public class EventVersionProperty
    {
        [Fact]
        public void ReturnsCorrectValue()
        {
            var domainEvent = new PrinterRegisteredEvent();

            domainEvent.EventVersion.Should().Be("1.0");
        }
    }

    public class ToJsonStringMethod
    {
        [Fact]
        public void SerializesCorrectly()
        {
            var printer = Printer.Register("TestEvent", "TestPrinter");
            var domainEvent = new PrinterRegisteredEvent(printer);

            var json = domainEvent.ToJsonString();

            json.Should().NotBeNullOrEmpty();
            var deserialized = JsonSerializer.Deserialize<PrinterRegisteredEvent>(json);
            deserialized.Should().NotBeNull();
            deserialized!.PrinterId.Should().Be("TESTEVENT-TESTPRINTER");
        }

        [Fact]
        public void IncludesEventNameInJson()
        {
            var domainEvent = new PrinterRegisteredEvent { PrinterId = "TEST-ID" };

            var json = domainEvent.ToJsonString();

            json.Should().Contain("printers.registered.v1");
        }

        [Fact]
        public void IncludesEventVersionInJson()
        {
            var domainEvent = new PrinterRegisteredEvent { PrinterId = "TEST-ID" };

            var json = domainEvent.ToJsonString();

            json.Should().Contain("1.0");
        }

        [Fact]
        public void IncludesPrinterIdInJson()
        {
            var domainEvent = new PrinterRegisteredEvent { PrinterId = "MYEVENT-MYPRINTER" };

            var json = domainEvent.ToJsonString();

            json.Should().Contain("MYEVENT-MYPRINTER");
        }
    }

    public class ParameterlessConstructor
    {
        [Fact]
        public void CreateInstanceWithEmptyPrinterId()
        {
            var domainEvent = new PrinterRegisteredEvent();

            domainEvent.PrinterId.Should().Be("");
        }

        [Fact]
        public void AllowsSettingPrinterIdAfterConstruction()
        {
            var domainEvent = new PrinterRegisteredEvent
            {
                PrinterId = "SET-LATER"
            };

            domainEvent.PrinterId.Should().Be("SET-LATER");
        }
    }
}
