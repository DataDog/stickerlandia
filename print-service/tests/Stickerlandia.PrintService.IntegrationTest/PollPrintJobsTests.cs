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
public sealed class PollPrintJobsTests(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    : IDisposable
{
    private readonly PrinterDriver _driver = new(testOutputHelper, testSetupFixture.HttpClient, new CookieContainer(), testSetupFixture.OidcServer);

    [Fact]
    public async Task WhenPollingWithValidApiKeyShouldReturnNoContentIfNoJobs()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");
        registerResult!.Key.Should().NotBeNullOrEmpty("Registration should return API key");

        // Act - Poll with no pending jobs
        var (statusCode, _) = await _driver.PollPrintJobs(registerResult.Key);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WhenPollingWithValidApiKeyShouldReturnQueuedJobs()
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

        // Act - Poll for jobs
        var (statusCode, response) = await _driver.PollPrintJobs(registerResult!.Key);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Jobs.Should().HaveCount(1);
        response.Jobs[0].PrintJobId.Should().Be(submitResponse!.PrintJobId);
        response.Jobs[0].UserId.Should().Be("test-user-123");
        response.Jobs[0].StickerId.Should().Be("sticker-456");
    }

    [Fact]
    public async Task WhenPollingWithoutApiKeyShouldReturnUnauthorized()
    {
        // Act
        var statusCode = await _driver.PollPrintJobsWithoutAuth();

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenPollingWithInvalidApiKeyShouldReturnUnauthorized()
    {
        // Act
        var statusCode = await _driver.PollPrintJobsWithInvalidApiKey();

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenPollingWithMultipleJobsShouldReturnAllQueuedJobs()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit multiple print jobs
        for (var i = 0; i < 3; i++)
        {
            var printJobRequest = new SubmitPrintJobRequest
            {
                UserId = $"test-user-{i}",
                StickerId = $"sticker-{i}",
                StickerUrl = $"https://example.com/stickers/{i}.png"
            };
            var (submitStatus, _) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
            submitStatus.Should().Be(HttpStatusCode.Created);
        }

        // Act - Poll for jobs
        var (statusCode, response) = await _driver.PollPrintJobs(registerResult!.Key);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Jobs.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenPollingWithMaxJobsParameterShouldRespectLimit()
    {
        // Arrange
        var eventName = $"TestEvent-{Guid.NewGuid():N}";
        var printerName = $"TestPrinter-{Guid.NewGuid():N}";
        var adminToken = _driver.GetAdminToken();

        // Register a printer and get the API key
        var registerResult = await _driver.RegisterPrinter(adminToken, eventName, printerName);
        registerResult.Should().NotBeNull("Printer registration should succeed");

        // Submit 5 print jobs
        for (var i = 0; i < 5; i++)
        {
            var printJobRequest = new SubmitPrintJobRequest
            {
                UserId = $"test-user-{i}",
                StickerId = $"sticker-{i}",
                StickerUrl = $"https://example.com/stickers/{i}.png"
            };
            var (submitStatus, _) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
            submitStatus.Should().Be(HttpStatusCode.Created);
        }

        // Act - Poll for only 2 jobs
        var (statusCode, response) = await _driver.PollPrintJobs(registerResult!.Key, maxJobs: 2);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response!.Jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenPollingTwiceJobsShouldOnlyBeReturnedOnce()
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
        var (submitStatus, _) = await _driver.SubmitPrintJob(adminToken, eventName, printerName, printJobRequest);
        submitStatus.Should().Be(HttpStatusCode.Created);

        // Act - Poll twice
        var (firstStatus, firstResponse) = await _driver.PollPrintJobs(registerResult!.Key);
        var (secondStatus, _) = await _driver.PollPrintJobs(registerResult.Key);

        // Assert
        firstStatus.Should().Be(HttpStatusCode.OK);
        firstResponse.Should().NotBeNull();
        firstResponse!.Jobs.Should().HaveCount(1);

        // Second poll should return no content as job was marked as Processing
        secondStatus.Should().Be(HttpStatusCode.NoContent);
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}
