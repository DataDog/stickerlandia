/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.GetPrinters;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class PrinterStatusDtoTests
{
    public class FromPrinterMethod
    {
        [Fact]
        public void MapsPrinterIdCorrectly()
        {
            var printer = CreatePrinter("TestEvent", "Printer1");

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.PrinterId.Should().Be("TESTEVENT-PRINTER1");
        }

        [Fact]
        public void MapsPrinterNameCorrectly()
        {
            var printer = CreatePrinter("TestEvent", "MyPrinter");

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.PrinterName.Should().Be("MyPrinter");
        }

        [Fact]
        public void MapsOnlineStatusCorrectly()
        {
            var printer = CreatePrinterWithHeartbeat("TestEvent", "Printer1", DateTimeOffset.UtcNow);

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.Status.Should().Be("Online");
        }

        [Fact]
        public void MapsOfflineStatusCorrectly()
        {
            var printer = CreatePrinterWithHeartbeat("TestEvent", "Printer1", DateTimeOffset.UtcNow.AddMinutes(-5));

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.Status.Should().Be("Offline");
        }

        [Fact]
        public void MapsLastHeartbeatCorrectly()
        {
            var lastHeartbeat = DateTimeOffset.UtcNow.AddMinutes(-1);
            var printer = CreatePrinterWithHeartbeat("TestEvent", "Printer1", lastHeartbeat);

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.LastHeartbeat.Should().Be(lastHeartbeat);
        }

        [Fact]
        public void MapsNullLastHeartbeatCorrectly()
        {
            var printer = CreatePrinter("TestEvent", "Printer1");

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.LastHeartbeat.Should().BeNull();
        }

        [Fact]
        public void MapsLastJobProcessedCorrectly()
        {
            var lastJobProcessed = DateTimeOffset.UtcNow.AddMinutes(-2);
            var printer = CreatePrinterWithTimestamps("TestEvent", "Printer1", DateTimeOffset.UtcNow, lastJobProcessed);

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.LastJobProcessed.Should().Be(lastJobProcessed);
        }

        [Fact]
        public void MapsNullLastJobProcessedCorrectly()
        {
            var printer = CreatePrinterWithHeartbeat("TestEvent", "Printer1", DateTimeOffset.UtcNow);

            var dto = PrinterStatusDto.FromPrinter(printer);

            dto.LastJobProcessed.Should().BeNull();
        }

        [Fact]
        public void WithNullPrinter_ThrowsArgumentNullException()
        {
            var action = () => PrinterStatusDto.FromPrinter(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        private static Printer CreatePrinter(string eventName, string printerName)
        {
            var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
            return Printer.From(printerId, eventName, printerName, "test-key");
        }

        private static Printer CreatePrinterWithHeartbeat(string eventName, string printerName, DateTimeOffset lastHeartbeat)
        {
            var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
            return Printer.From(printerId, eventName, printerName, "test-key", lastHeartbeat);
        }

        private static Printer CreatePrinterWithTimestamps(string eventName, string printerName, DateTimeOffset lastHeartbeat, DateTimeOffset lastJobProcessed)
        {
            var printerId = new PrinterId($"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}");
            return Printer.From(printerId, eventName, printerName, "test-key", lastHeartbeat, lastJobProcessed);
        }
    }
}
