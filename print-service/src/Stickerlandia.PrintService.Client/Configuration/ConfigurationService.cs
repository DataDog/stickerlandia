// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application
#pragma warning disable CA1515, CA1031, CA1848, CA1812

using System.Text.Json;

namespace Stickerlandia.PrintService.Client.Configuration;

/// <summary>
/// File-based configuration service for the printer client.
/// </summary>
internal sealed class ConfigurationService : IConfigurationService, IDisposable
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".stickerlandia");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "printer-config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ConfigurationService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private PrinterClientConfig _current = new();

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    public PrinterClientConfig Current => _current;

    public bool IsConfigured => _current.IsConfigured;

    public event Action? OnConfigurationChanged;

    public async Task LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                _logger.LogInformation("No configuration file found at {Path}, using defaults", ConfigFilePath);
                return;
            }

            var json = await File.ReadAllTextAsync(ConfigFilePath);
            var config = JsonSerializer.Deserialize<PrinterClientConfig>(json, JsonOptions);

            if (config != null)
            {
                _current = config;
                _logger.LogInformation("Configuration loaded from {Path}", ConfigFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", ConfigFilePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(PrinterClientConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        await _lock.WaitAsync();
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(ConfigDirectory);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigFilePath, json);

            _current = config;
            _logger.LogInformation("Configuration saved to {Path}", ConfigFilePath);

            OnConfigurationChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", ConfigFilePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
