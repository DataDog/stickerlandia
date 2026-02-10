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
public sealed class PrintJobTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly PrinterDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient, new CookieContainer(), testSetupFixture.OidcServer);

    [Fact]
    public async Task WhenAPrintJobIsSubmittedToExistingPrinterItShouldReturnPrintJobId()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var authToken = _driver.GetAdminToken();

        // First register the printer
        var registerResult = await _driver.RegisterPrinter(authToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };

        // Act
        var (statusCode, response) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Created);
        response.Should().NotBeNull();
        response!.PrintJobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WhenAPrintJobIsSubmittedByUserRoleItShouldSucceed()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();
        var userToken = _driver.GetUserToken();

        // First register the printer as admin
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };

        // Act - submit as user role
        var (statusCode, response) = await _driver.SubmitPrintJob(userToken, eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Created);
        response.Should().NotBeNull();
        response!.PrintJobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WhenAPrintJobIsSubmittedToNonExistentPrinterItShouldReturnNotFound()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"NonExistentPrinter-{Guid.NewGuid():N}";
        var authToken = _driver.GetAdminToken();

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };

        // Act
        var (statusCode, response) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
        response.Should().BeNull();
    }

    [Fact]
    public async Task WhenAPrintJobIsSubmittedWithoutAuthenticationItShouldReturnUnauthorized()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };

        // Act
        var statusCode = await _driver.SubmitPrintJobWithoutAuth(eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAPrintJobIsSubmittedWithInvalidUrlItShouldReturnBadRequest()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var authToken = _driver.GetAdminToken();

        // First register the printer
        var registerResult = await _driver.RegisterPrinter(authToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "not-a-valid-url"
        };

        // Act
        var (statusCode, response) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Should().BeNull();
    }

    [Fact]
    public async Task WhenAPrintJobIsSubmittedWithMissingUserIdItShouldReturnBadRequest()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var authToken = _driver.GetAdminToken();

        // First register the printer
        var registerResult = await _driver.RegisterPrinter(authToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };

        // Act
        var (statusCode, response) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Should().BeNull();
    }

    [Fact]
    public async Task WhenMultiplePrintJobsAreSubmittedToSamePrinterEachShouldHaveUniquePrintJobId()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var authToken = _driver.GetAdminToken();

        // First register the printer
        var registerResult = await _driver.RegisterPrinter(authToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        var printJobRequest1 = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-001",
            StickerUrl = "https://example.com/stickers/sticker1.png"
        };

        var printJobRequest2 = new SubmitPrintJobRequest
        {
            UserId = "test-user-456",
            StickerId = "sticker-002",
            StickerUrl = "https://example.com/stickers/sticker2.png"
        };

        // Act
        var (statusCode1, response1) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest1);
        var (statusCode2, response2) = await _driver.SubmitPrintJob(authToken, eventName, printerName, printJobRequest2);

        // Assert
        statusCode1.Should().Be(HttpStatusCode.Created);
        statusCode2.Should().Be(HttpStatusCode.Created);
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response1!.PrintJobId.Should().NotBe(response2!.PrintJobId);
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}
