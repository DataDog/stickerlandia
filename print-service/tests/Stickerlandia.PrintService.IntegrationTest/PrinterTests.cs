/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Net;
using FluentAssertions;
using Stickerlandia.PrintService.IntegrationTest.Drivers;
using Stickerlandia.PrintService.IntegrationTest.Hooks;
using Xunit.Abstractions;

namespace Stickerlandia.PrintService.IntegrationTest;

[Collection("Integration Tests")]
public sealed class PrinterTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly PrinterDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient, new CookieContainer());

    [Fact]
    public async Task WhenAPrinterIsRegisteredItShouldBeRetrievableByEvent()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var authToken = PrinterDriver.GetAdminToken();

        // Act
        var registerResult = await _driver.RegisterPrinter(authToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printers = await _driver.GetPrintersForEvent(authToken, eventName);

        // Assert
        printers.Should().NotBeNull();
        printers.Should().ContainSingle();
        printers![0].PrinterName.Should().Be(printerName);
        printers[0].EventName.Should().Be(eventName);
    }

    [Fact]
    public async Task WhenMultiplePrintersAreRegisteredAllShouldBeRetrievableByEvent()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName1 = $"TestPrinter1-{Guid.NewGuid():N}";
        var printerName2 = $"TestPrinter2-{Guid.NewGuid():N}";
        var authToken = PrinterDriver.GetAdminToken();

        // Act
        await _driver.RegisterPrinter(authToken, eventName, printerName1);
        await _driver.RegisterPrinter(authToken, eventName, printerName2);

        var printers = await _driver.GetPrintersForEvent(authToken, eventName);

        // Assert
        printers.Should().NotBeNull();
        printers.Should().HaveCount(2);
        printers.Should().Contain(p => p.PrinterName == printerName1);
        printers.Should().Contain(p => p.PrinterName == printerName2);
    }

    [Fact]
    public async Task WhenNoPrintersExistForEventEmptyListShouldBeReturned()
    {
        // Arrange
        var eventName = $"EmptyEvent-{Guid.NewGuid():N}";
        var authToken = PrinterDriver.GetAdminToken();

        // Act
        var printers = await _driver.GetPrintersForEvent(authToken, eventName);

        // Assert
        printers.Should().NotBeNull();
        printers.Should().BeEmpty();
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}
