// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Models;
using Stickerlandia.PrintService.Client.Services;

namespace Stickerlandia.PrintService.Client.Tests;

public class PrintServiceApiClientTests : IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<PrintServiceApiClient> _logger;
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly PrintServiceApiClient _client;

    public PrintServiceApiClientTests()
    {
        _configService = A.Fake<IConfigurationService>();
        A.CallTo(() => _configService.Current).Returns(new PrinterClientConfig
        {
            ApiKey = "test-api-key",
            BackendUrl = "https://api.test.com"
        });
        A.CallTo(() => _configService.IsConfigured).Returns(true);

        _logger = A.Fake<ILogger<PrintServiceApiClient>>();
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _client = new PrintServiceApiClient(_httpClient, _configService, _logger);
    }

    [Fact]
    public async Task PollJobsAsync_WithJobs_ShouldReturnJobs()
    {
        // Arrange
        var response = new ApiResponse<PollJobsResponse>
        {
            Data = new PollJobsResponse
            {
                Jobs = new List<PrintJobDto>
                {
                    new()
                    {
                        PrintJobId = "job-123",
                        UserId = "user-456",
                        StickerId = "sticker-789",
                        StickerUrl = "https://example.com/sticker.png"
                    }
                }
            }
        };
        _mockHandler.SetResponse(HttpStatusCode.OK, response);

        // Act
        var result = await _client.PollJobsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].PrintJobId.Should().Be("job-123");
    }

    [Fact]
    public async Task PollJobsAsync_WithNoContent_ShouldReturnEmptyList()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.NoContent);

        // Act
        var result = await _client.PollJobsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PollJobsAsync_WithUnauthorized_ShouldReturnEmptyList()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.Unauthorized);

        // Act
        var result = await _client.PollJobsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PollJobsAsync_WhenNotConfigured_ShouldReturnEmptyList()
    {
        // Arrange
        A.CallTo(() => _configService.IsConfigured).Returns(false);

        // Act
        var result = await _client.PollJobsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AcknowledgeJobAsync_WithSuccess_ShouldReturnTrue()
    {
        // Arrange
        var response = new ApiResponse<AcknowledgeJobResponse>
        {
            Data = new AcknowledgeJobResponse { Acknowledged = true }
        };
        _mockHandler.SetResponse(HttpStatusCode.OK, response);

        // Act
        var result = await _client.AcknowledgeJobAsync("job-123", success: true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AcknowledgeJobAsync_WithFailure_ShouldReturnTrue()
    {
        // Arrange
        var response = new ApiResponse<AcknowledgeJobResponse>
        {
            Data = new AcknowledgeJobResponse { Acknowledged = true }
        };
        _mockHandler.SetResponse(HttpStatusCode.OK, response);

        // Act
        var result = await _client.AcknowledgeJobAsync("job-123", success: false, "Paper jam");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AcknowledgeJobAsync_WithNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.NotFound);

        // Act
        var result = await _client.AcknowledgeJobAsync("job-123", success: true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AcknowledgeJobAsync_WhenNotConfigured_ShouldReturnFalse()
    {
        // Arrange
        A.CallTo(() => _configService.IsConfigured).Returns(false);

        // Act
        var result = await _client.AcknowledgeJobAsync("job-123", success: true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AcknowledgeJobAsync_WithEmptyJobId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _client.AcknowledgeJobAsync("", success: true);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithValidConnection_ShouldReturnPrinterInfo()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.NoContent);

        // Act
        var result = await _client.ValidateConnectionAsync();

        // Assert
        result.Should().NotBeNull();
        result!.PrinterId.Should().Be("connected");
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithUnauthorized_ShouldReturnNull()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.Unauthorized);

        // Act
        var result = await _client.ValidateConnectionAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateConnectionAsync_WhenNotConfigured_ShouldReturnNull()
    {
        // Arrange
        A.CallTo(() => _configService.IsConfigured).Returns(false);

        // Act
        var result = await _client.ValidateConnectionAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PollJobsAsync_ShouldSendCorrectApiKeyHeader()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.NoContent);

        // Act
        await _client.PollJobsAsync();

        // Assert
        _mockHandler.LastRequest.Should().NotBeNull();
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "X-Printer-Key");
        _mockHandler.LastRequest.Headers.GetValues("X-Printer-Key").First().Should().Be("test-api-key");
    }

    [Fact]
    public async Task PollJobsAsync_ShouldUseCorrectBackendUrl()
    {
        // Arrange
        _mockHandler.SetResponse(HttpStatusCode.NoContent);

        // Act
        await _client.PollJobsAsync();

        // Assert
        _mockHandler.LastRequest.Should().NotBeNull();
        _mockHandler.LastRequest!.RequestUri!.ToString().Should().StartWith("https://api.test.com/");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }
}

/// <summary>
/// Mock HTTP message handler for testing.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private object? _responseContent;

    public HttpRequestMessage? LastRequest { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, object? content = null)
    {
        _statusCode = statusCode;
        _responseContent = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = new HttpResponseMessage(_statusCode);

        if (_responseContent != null)
        {
            var json = JsonSerializer.Serialize(_responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }
}
