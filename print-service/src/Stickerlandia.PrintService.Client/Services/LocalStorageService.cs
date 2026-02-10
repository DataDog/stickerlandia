// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

// Suppress code analysis for client application
#pragma warning disable CA1515, CA1848, CA1031, CA1812

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Stickerlandia.PrintService.Client.Configuration;
using Stickerlandia.PrintService.Client.Models;

namespace Stickerlandia.PrintService.Client.Services;

/// <summary>
/// File-based local storage service for print job records.
/// </summary>
internal sealed class LocalStorageService : ILocalStorageService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IConfigurationService _configService;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly ConcurrentDictionary<string, PrintJobRecord> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _initialized;

    public LocalStorageService(IConfigurationService configService, ILogger<LocalStorageService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    private string StoragePath => _configService.Current.LocalStoragePath;

    public async Task StoreJobAsync(PrintJobDto job)
    {
        ArgumentNullException.ThrowIfNull(job);

        await EnsureInitializedAsync();

        var record = new PrintJobRecord
        {
            PrintJobId = job.PrintJobId,
            UserId = job.UserId,
            StickerId = job.StickerId,
            StickerUrl = job.StickerUrl,
            ReceivedAt = DateTimeOffset.UtcNow,
            Status = "Received"
        };

        _cache[job.PrintJobId] = record;
        await SaveJobFileAsync(record);

        _logger.LogInformation("Stored job {JobId}", job.PrintJobId);
    }

    public async Task MarkCompletedAsync(string printJobId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        await EnsureInitializedAsync();

        if (_cache.TryGetValue(printJobId, out var record))
        {
            var updated = record with
            {
                Status = "Completed",
                CompletedAt = DateTimeOffset.UtcNow
            };
            _cache[printJobId] = updated;
            await SaveJobFileAsync(updated);

            _logger.LogInformation("Marked job {JobId} as completed", printJobId);
        }
    }

    public async Task MarkFailedAsync(string printJobId, string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        await EnsureInitializedAsync();

        if (_cache.TryGetValue(printJobId, out var record))
        {
            var updated = record with
            {
                Status = "Failed",
                CompletedAt = DateTimeOffset.UtcNow,
                FailureReason = reason
            };
            _cache[printJobId] = updated;
            await SaveJobFileAsync(updated);

            _logger.LogInformation("Marked job {JobId} as failed: {Reason}", printJobId, reason);
        }
    }

    public async Task MarkAcknowledgedAsync(string printJobId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        await EnsureInitializedAsync();

        if (_cache.TryGetValue(printJobId, out var record))
        {
            var updated = record with { Acknowledged = true };
            _cache[printJobId] = updated;
            await SaveJobFileAsync(updated);

            _logger.LogDebug("Marked job {JobId} as acknowledged", printJobId);
        }
    }

    public async Task<PrintJobRecord?> GetJobAsync(string printJobId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        await EnsureInitializedAsync();

        return _cache.GetValueOrDefault(printJobId);
    }

    public async Task<IReadOnlyList<PrintJobRecord>> GetJobsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, string? status = null)
    {
        await EnsureInitializedAsync();

        var query = _cache.Values.AsEnumerable();

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(j => j.ReceivedAt >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(j => j.ReceivedAt <= to);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(j => j.ReceivedAt).ToList();
    }

    public async Task<IReadOnlyList<PrintJobRecord>> GetUnacknowledgedJobsAsync()
    {
        await EnsureInitializedAsync();

        return _cache.Values
            .Where(j => !j.Acknowledged && (j.Status == "Completed" || j.Status == "Failed"))
            .ToList();
    }

    public async Task<int> GetJobsProcessedTodayAsync()
    {
        await EnsureInitializedAsync();

        var today = DateTimeOffset.UtcNow.Date;
        return _cache.Values.Count(j => j.ReceivedAt.Date == today);
    }

    public async Task<int> GetTotalJobsProcessedAsync()
    {
        await EnsureInitializedAsync();

        return _cache.Count;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _lock.WaitAsync();
        try
        {
            if (_initialized) return;

            await LoadAllJobsAsync();
            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadAllJobsAsync()
    {
        if (!Directory.Exists(StoragePath))
        {
            Directory.CreateDirectory(StoragePath);
            _logger.LogInformation("Created storage directory at {Path}", StoragePath);
            return;
        }

        var files = Directory.GetFiles(StoragePath, "*.json", SearchOption.AllDirectories);
        _logger.LogInformation("Loading {Count} job files from storage", files.Length);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var record = JsonSerializer.Deserialize<PrintJobRecord>(json, JsonOptions);
                if (record != null)
                {
                    _cache[record.PrintJobId] = record;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load job file {File}", file);
            }
        }

        _logger.LogInformation("Loaded {Count} jobs into cache", _cache.Count);
    }

    private async Task SaveJobFileAsync(PrintJobRecord record)
    {
        var dateFolder = record.ReceivedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var folderPath = Path.Combine(StoragePath, dateFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, $"{record.PrintJobId}.json");
        var json = JsonSerializer.Serialize(record, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
