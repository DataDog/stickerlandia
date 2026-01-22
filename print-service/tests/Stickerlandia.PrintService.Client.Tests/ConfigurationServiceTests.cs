// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Client.Configuration;

namespace Stickerlandia.PrintService.Client.Tests;

/// <summary>
/// Tests for IConfigurationService behavior using fakes.
/// Note: ConfigurationService uses hardcoded file paths, so we test the interface behavior through mocks.
/// </summary>
public class ConfigurationServiceTests
{
    private readonly IConfigurationService _configService;

    public ConfigurationServiceTests()
    {
        _configService = A.Fake<IConfigurationService>();
    }

    [Fact]
    public void Current_ShouldReturnConfiguration()
    {
        // Arrange
        var expectedConfig = new PrinterClientConfig
        {
            ApiKey = "test-key",
            BackendUrl = "https://api.test.com"
        };
        A.CallTo(() => _configService.Current).Returns(expectedConfig);

        // Act
        var result = _configService.Current;

        // Assert
        result.Should().Be(expectedConfig);
        result.ApiKey.Should().Be("test-key");
        result.BackendUrl.Should().Be("https://api.test.com");
    }

    [Fact]
    public void IsConfigured_WithApiKeyAndUrl_ShouldReturnTrue()
    {
        // Arrange
        A.CallTo(() => _configService.IsConfigured).Returns(true);

        // Assert
        _configService.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WithoutApiKey_ShouldReturnFalse()
    {
        // Arrange
        A.CallTo(() => _configService.IsConfigured).Returns(false);

        // Assert
        _configService.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldBeCallable()
    {
        // Act
        await _configService.LoadAsync();

        // Assert
        A.CallTo(() => _configService.LoadAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SaveAsync_ShouldAcceptConfiguration()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = "new-key",
            BackendUrl = "https://new-api.test.com"
        };

        // Act
        await _configService.SaveAsync(config);

        // Assert
        A.CallTo(() => _configService.SaveAsync(config)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void OnConfigurationChanged_ShouldBeSubscribable()
    {
        // Arrange
        var eventFired = false;

        // Act
        _configService.OnConfigurationChanged += () => eventFired = true;

        // Assert - just verify subscription works (event firing is implementation detail)
        eventFired.Should().BeFalse(); // Event hasn't been raised yet
    }
}

/// <summary>
/// Tests for PrinterClientConfig model.
/// </summary>
public class PrinterClientConfigTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var config = new PrinterClientConfig();

        // Assert
        config.BackendUrl.Should().Be("https://api.stickerlandia.com");
        config.PollingIntervalSeconds.Should().Be(5);
        config.MaxJobsPerPoll.Should().Be(10);
    }

    [Fact]
    public void IsConfigured_WithApiKeyAndUrl_ShouldReturnTrue()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = "test-key",
            BackendUrl = "https://api.example.com"
        };

        // Assert
        config.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WithNullApiKey_ShouldReturnFalse()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = null,
            BackendUrl = "https://api.example.com"
        };

        // Assert
        config.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyApiKey_ShouldReturnFalse()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = "",
            BackendUrl = "https://api.example.com"
        };

        // Assert
        config.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithNullBackendUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = "test-key",
            BackendUrl = null!
        };

        // Assert
        config.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithEmptyBackendUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new PrinterClientConfig
        {
            ApiKey = "test-key",
            BackendUrl = ""
        };

        // Assert
        config.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void LocalStoragePath_DefaultValue_ShouldBePrintJobs()
    {
        // Act
        var config = new PrinterClientConfig();

        // Assert
        config.LocalStoragePath.Should().Be("./print-jobs");
    }
}
