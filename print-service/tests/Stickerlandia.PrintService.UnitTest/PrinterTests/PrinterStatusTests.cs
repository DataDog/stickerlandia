/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.UnitTest.PrinterTests;

public class PrinterStatusTests
{
    [Fact]
    public void StatusWhenNoHeartbeatShouldBeOffline()
    {
        // Arrange
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat: null);

        // Act & Assert
        printer.Status.Should().Be(PrinterStatus.Offline);
    }

    [Fact]
    public void StatusWhenHeartbeatWithinTwoMinutesShouldBeOnline()
    {
        // Arrange
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat: DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act & Assert
        printer.Status.Should().Be(PrinterStatus.Online);
    }

    [Fact]
    public void StatusWhenHeartbeatJustUnderTwoMinutesAgoShouldBeOnline()
    {
        // Arrange - Use 1 minute 59 seconds to avoid timing edge cases
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat: DateTimeOffset.UtcNow.AddSeconds(-119));

        // Act & Assert
        printer.Status.Should().Be(PrinterStatus.Online);
    }

    [Fact]
    public void StatusWhenHeartbeatOlderThanTwoMinutesShouldBeOffline()
    {
        // Arrange
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat: DateTimeOffset.UtcNow.AddMinutes(-3));

        // Act & Assert
        printer.Status.Should().Be(PrinterStatus.Offline);
    }

    [Fact]
    public void RecordHeartbeatShouldUpdateLastHeartbeatTimestamp()
    {
        // Arrange
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat: null);

        // Act
        printer.RecordHeartbeat();

        // Assert
        printer.LastHeartbeat.Should().NotBeNull();
        printer.LastHeartbeat.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        printer.Status.Should().Be(PrinterStatus.Online);
    }

    [Fact]
    public void RecordJobProcessedShouldUpdateLastJobProcessedTimestamp()
    {
        // Arrange
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123");

        // Act
        printer.RecordJobProcessed();

        // Assert
        printer.LastJobProcessed.Should().NotBeNull();
        printer.LastJobProcessed.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PrinterFromShouldPreserveHeartbeatTimestamps()
    {
        // Arrange
        var lastHeartbeat = DateTimeOffset.UtcNow.AddMinutes(-1);
        var lastJobProcessed = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var printer = Printer.From(
            new PrinterId("EVENT-PRINTER"),
            "TestEvent",
            "TestPrinter",
            "testkey123",
            lastHeartbeat,
            lastJobProcessed);

        // Assert
        printer.LastHeartbeat.Should().Be(lastHeartbeat);
        printer.LastJobProcessed.Should().Be(lastJobProcessed);
    }
}
