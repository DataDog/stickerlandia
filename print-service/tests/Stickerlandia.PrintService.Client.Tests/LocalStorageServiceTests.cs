// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Models;
using Stickerlandia.PrintService.Client.Services;

namespace Stickerlandia.PrintService.Client.Tests;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _testStoragePath;
    private readonly IConfigurationService _configService;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly LocalStorageService _service;

    public LocalStorageServiceTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), $"stickerlandia-storage-test-{Guid.NewGuid():N}");

        _configService = A.Fake<IConfigurationService>();
        A.CallTo(() => _configService.Current).Returns(new PrinterClientConfig
        {
            LocalStoragePath = _testStoragePath
        });

        _logger = A.Fake<ILogger<LocalStorageService>>();
        _service = new LocalStorageService(_configService, _logger);
    }

    [Fact]
    public async Task StoreJobAsync_ShouldStoreJob()
    {
        // Arrange
        var job = CreateTestJob();

        // Act
        await _service.StoreJobAsync(job);

        // Assert
        var retrieved = await _service.GetJobAsync(job.PrintJobId);
        retrieved.Should().NotBeNull();
        retrieved!.PrintJobId.Should().Be(job.PrintJobId);
        retrieved.UserId.Should().Be(job.UserId);
        retrieved.StickerId.Should().Be(job.StickerId);
        retrieved.Status.Should().Be("Received");
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldUpdateStatus()
    {
        // Arrange
        var job = CreateTestJob();
        await _service.StoreJobAsync(job);

        // Act
        await _service.MarkCompletedAsync(job.PrintJobId);

        // Assert
        var retrieved = await _service.GetJobAsync(job.PrintJobId);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
        retrieved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldUpdateStatusAndReason()
    {
        // Arrange
        var job = CreateTestJob();
        await _service.StoreJobAsync(job);

        // Act
        await _service.MarkFailedAsync(job.PrintJobId, "Paper jam");

        // Assert
        var retrieved = await _service.GetJobAsync(job.PrintJobId);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Failed");
        retrieved.FailureReason.Should().Be("Paper jam");
        retrieved.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAcknowledgedAsync_ShouldSetAcknowledgedFlag()
    {
        // Arrange
        var job = CreateTestJob();
        await _service.StoreJobAsync(job);
        await _service.MarkCompletedAsync(job.PrintJobId);

        // Act
        await _service.MarkAcknowledgedAsync(job.PrintJobId);

        // Assert
        var retrieved = await _service.GetJobAsync(job.PrintJobId);
        retrieved.Should().NotBeNull();
        retrieved!.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public async Task GetUnacknowledgedJobsAsync_ShouldReturnOnlyUnacknowledgedCompletedOrFailed()
    {
        // Arrange
        var job1 = CreateTestJob("job1");
        var job2 = CreateTestJob("job2");
        var job3 = CreateTestJob("job3");

        await _service.StoreJobAsync(job1);
        await _service.StoreJobAsync(job2);
        await _service.StoreJobAsync(job3);

        await _service.MarkCompletedAsync(job1.PrintJobId);
        await _service.MarkFailedAsync(job2.PrintJobId, "Error");
        // job3 stays as "Received"

        await _service.MarkAcknowledgedAsync(job1.PrintJobId);
        // job2 is not acknowledged

        // Act
        var unacknowledged = await _service.GetUnacknowledgedJobsAsync();

        // Assert
        unacknowledged.Should().HaveCount(1);
        unacknowledged[0].PrintJobId.Should().Be("job2");
    }

    [Fact]
    public async Task GetJobsProcessedTodayAsync_ShouldReturnCountOfTodaysJobs()
    {
        // Arrange
        await _service.StoreJobAsync(CreateTestJob("job1"));
        await _service.StoreJobAsync(CreateTestJob("job2"));
        await _service.StoreJobAsync(CreateTestJob("job3"));

        // Act
        var count = await _service.GetJobsProcessedTodayAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalJobsProcessedAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await _service.StoreJobAsync(CreateTestJob("job1"));
        await _service.StoreJobAsync(CreateTestJob("job2"));

        // Act
        var count = await _service.GetTotalJobsProcessedAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetJobsAsync_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        await _service.StoreJobAsync(CreateTestJob("job1"));
        await _service.StoreJobAsync(CreateTestJob("job2"));
        await _service.MarkCompletedAsync("job1");

        // Act
        var completedJobs = await _service.GetJobsAsync(status: "Completed");
        var receivedJobs = await _service.GetJobsAsync(status: "Received");

        // Assert
        completedJobs.Should().HaveCount(1);
        completedJobs[0].PrintJobId.Should().Be("job1");

        receivedJobs.Should().HaveCount(1);
        receivedJobs[0].PrintJobId.Should().Be("job2");
    }

    [Fact]
    public async Task GetJobAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetJobAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreJobAsync_WithNullJob_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.StoreJobAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static PrintJobDto CreateTestJob(string? id = null)
    {
        return new PrintJobDto
        {
            PrintJobId = id ?? Guid.NewGuid().ToString(),
            UserId = "test-user",
            StickerId = "sticker-123",
            StickerUrl = "https://example.com/sticker.png"
        };
    }

    public void Dispose()
    {
        _service.Dispose();

        if (Directory.Exists(_testStoragePath))
        {
            try
            {
                Directory.Delete(_testStoragePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
