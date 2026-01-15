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
public sealed class AcknowledgePrintJobTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly PrinterDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient, new CookieContainer(), testSetupFixture.OidcServer);

    [Fact]
    public async Task WhenAcknowledgingSuccessfullyShouldReturnOk()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit a print job
        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };
        var (submitStatus, submitResponse) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
        submitStatus.Should().Be(HttpStatusCode.Created);

        // Poll to get the job (moves it to Processing)
        var (pollStatus, pollResponse) = await _driver.PollPrintJobs(registerResult!.Key);
        pollStatus.Should().Be(HttpStatusCode.OK);

        // Act - Acknowledge the job
        var (statusCode, response) = await _driver.AcknowledgePrintJob(registerResult.Key, submitResponse!.PrintJobId, success: true);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task WhenAcknowledgingWithFailureShouldReturnOk()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit a print job
        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };
        var (submitStatus, submitResponse) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
        submitStatus.Should().Be(HttpStatusCode.Created);

        // Poll to get the job (moves it to Processing)
        var (pollStatus, _) = await _driver.PollPrintJobs(registerResult!.Key);
        pollStatus.Should().Be(HttpStatusCode.OK);

        // Act - Acknowledge the job as failed
        var (statusCode, response) = await _driver.AcknowledgePrintJob(
            registerResult.Key,
            submitResponse!.PrintJobId,
            success: false,
            failureReason: "Paper jam");

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task WhenAcknowledgingWithoutApiKeyShouldReturnUnauthorized()
    {
        // Act
        var statusCode = await _driver.AcknowledgePrintJobWithoutAuth("some-job-id", success: true);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAcknowledgingNonExistentJobShouldReturnNotFound()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Act - Try to acknowledge a non-existent job
        var (statusCode, _) = await _driver.AcknowledgePrintJob(registerResult!.Key, "non-existent-job-id", success: true);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenAcknowledgingJobTwiceShouldReturnBadRequest()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit a print job
        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };
        var (submitStatus, submitResponse) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
        submitStatus.Should().Be(HttpStatusCode.Created);

        // Poll to get the job (moves it to Processing)
        var (pollStatus, _) = await _driver.PollPrintJobs(registerResult!.Key);
        pollStatus.Should().Be(HttpStatusCode.OK);

        // Acknowledge the job once
        var (firstStatus, _) = await _driver.AcknowledgePrintJob(registerResult.Key, submitResponse!.PrintJobId, success: true);
        firstStatus.Should().Be(HttpStatusCode.OK);

        // Act - Try to acknowledge again
        var (secondStatus, _) = await _driver.AcknowledgePrintJob(registerResult.Key, submitResponse.PrintJobId, success: true);

        // Assert - Job is no longer in Processing status
        secondStatus.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenAcknowledgingQueuedJobShouldReturnBadRequest()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit a print job (but don't poll it, so it stays Queued)
        var printJobRequest = new SubmitPrintJobRequest
        {
            UserId = "test-user-123",
            StickerId = "sticker-456",
            StickerUrl = "https://example.com/stickers/test.png"
        };
        var (submitStatus, submitResponse) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
        submitStatus.Should().Be(HttpStatusCode.Created);

        // Act - Try to acknowledge a Queued job (should fail)
        var (statusCode, _) = await _driver.AcknowledgePrintJob(registerResult!.Key, submitResponse!.PrintJobId, success: true);

        // Assert - Job is not in Processing status
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}
