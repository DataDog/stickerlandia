/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

using System.Net;
using FluentAssertions;
using Stickerlandia.PrintService.IntegrationTest.Drivers;
using Stickerlandia.PrintService.IntegrationTest.Hooks;
using Stickerlandia.PrintService.IntegrationTest.ViewModels;
using Xunit.Abstractions;

namespace Stickerlandia.PrintService.IntegrationTest;

[Collection("Integration Tests")]
public sealed class PrinterStatusTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly PrinterDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient, new CookieContainer(), testSetupFixture.OidcServer);

    [Fact]
    public async Task WhenGettingStatusesForEventWithPrintersShouldReturnStatuses()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Act
        var (statusCode, response) = await _driver.GetPrinterStatuses(adminToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().HaveCount(1);
        response.Printers[0].PrinterName.Should().Be(printerName);
    }

    [Fact]
    public async Task WhenGettingStatusesForEventWithNoPrintersShouldReturnEmptyList()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Act
        var (statusCode, response) = await _driver.GetPrinterStatuses(adminToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGettingStatusesWithoutAuthShouldReturnUnauthorized()
    {
        // Act
        var statusCode = await _driver.GetPrinterStatusesWithoutAuth("TestEvent");

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenPrinterPollsShouldShowOnlineStatus()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Poll to send heartbeat (this updates the LastHeartbeat)
        await _driver.PollPrintJobs(registerResult!.Key);

        // Act
        var (statusCode, response) = await _driver.GetPrinterStatuses(adminToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().HaveCount(1);
        response.Printers[0].Status.Should().Be("Online");
        response.Printers[0].LastHeartbeat.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenPrinterNeverPolledShouldShowOfflineStatus()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer but don't poll
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Act
        var (statusCode, response) = await _driver.GetPrinterStatuses(adminToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().HaveCount(1);
        response.Printers[0].Status.Should().Be("Offline");
        response.Printers[0].LastHeartbeat.Should().BeNull();
    }

    [Fact]
    public async Task WhenMultiplePrintersRegisteredShouldReturnAllStatuses()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register multiple printers
        var printer1 = await _driver.RegisterPrinter(adminToken, eventName, $"Printer1-{Guid.NewGuid():N}");
        var printer2 = await _driver.RegisterPrinter(adminToken, eventName, $"Printer2-{Guid.NewGuid():N}");
        var printer3 = await _driver.RegisterPrinter(adminToken, eventName, $"Printer3-{Guid.NewGuid():N}");

        // Poll with only the first printer
        await _driver.PollPrintJobs(printer1!.Key);

        // Act
        var (statusCode, response) = await _driver.GetPrinterStatuses(adminToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().HaveCount(3);

        // At least one should be online (the one that polled)
        response.Printers.Should().Contain(p => p.Status == "Online");
    }

    [Fact]
    public async Task WhenUserRoleRequestsStatusesShouldSucceed()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();
        var userToken = _driver.GetUserToken();

        // Register a printer (as admin)
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Act - Get statuses as user
        var (statusCode, response) = await _driver.GetPrinterStatuses(userToken, eventName);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Printers.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}
