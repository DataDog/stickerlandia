// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.AWS;

public class DynamoDbPrintJobRepository(
    IAmazonDynamoDB dynamoDbClient,
    DynamoDbWriteTransaction transaction,
    IOptions<AwsConfiguration> configuration) : IPrintJobRepository
{
    private const string PartitionKey = "PK";
    private const string SortKey = "SK";
    private const string Gsi1PartitionKey = "GSI1PK";
    private const string Gsi1SortKey = "GSI1SK";

    private readonly string _tableName = configuration.Value.PrintJobTableName;

    // TTL duration for completed/failed jobs: 2 days
    private static readonly TimeSpan TtlDuration = TimeSpan.FromDays(2);

    /// <summary>Buffered in transaction scope — committed via CommitAsync.</summary>
    public Task<PrintJob> AddAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var item = MapToItem(printJob);

        transaction.AddPut(_tableName, item);

        return Task.FromResult(printJob);
    }

    public async Task<PrintJob?> GetByIdAsync(string printJobId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printJobId);

        // We need to scan since we don't know the PrinterId
        var request = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "PrintJobId = :printJobId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":printJobId"] = new() { S = printJobId }
            }
        };

        var response = await dynamoDbClient.ScanAsync(request).ConfigureAwait(false);

        if (response.Items.Count == 0)
        {
            return null;
        }

        return MapToPrintJob(response.Items[0]);
    }

    public async Task<List<PrintJob>> GetQueuedJobsForPrinterAsync(string printerId, int maxJobs = 10)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        // Query GSI1 for queued jobs for this printer, ordered by creation time
        var gsi1Pk = $"PRINTER#{printerId}#STATUS#Queued";

        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :gsi1pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gsi1pk"] = new() { S = gsi1Pk }
            },
            Limit = maxJobs,
            ScanIndexForward = true // Oldest first (FIFO)
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        var jobs = new List<PrintJob>();

        foreach (var item in response.Items)
        {
            var job = MapToPrintJob(item);

            // Attempt to atomically claim the job (optimistic locking)
            var claimed = await TryClaimJobAsync(job);

            if (claimed)
            {
                job.MarkAsProcessing();
                jobs.Add(job);
            }
        }

        return jobs;
    }

    /// <summary>Buffered in transaction scope — committed via CommitAsync.</summary>
    public Task UpdateAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var item = MapToItem(printJob);

        transaction.AddPut(_tableName, item);

        return Task.CompletedTask;
    }

    /// <summary>Immediate BatchWriteItem — executes outside transaction scope.</summary>
    public async Task DeleteJobsForPrinterAsync(string printerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        var pk = $"PRINTER#{printerId}";

        // Query all jobs for this printer
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = pk }
            },
            ProjectionExpression = "PK, SK"
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        // Batch delete in groups of 25 (DynamoDB limit)
        foreach (var batch in response.Items.Chunk(25))
        {
            var writeRequests = batch.Select(item => new WriteRequest
            {
                DeleteRequest = new DeleteRequest
                {
                    Key = new Dictionary<string, AttributeValue>
                    {
                        [PartitionKey] = item[PartitionKey],
                        [SortKey] = item[SortKey]
                    }
                }
            }).ToList();

            var batchRequest = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [_tableName] = writeRequests
                }
            };

            await dynamoDbClient.BatchWriteItemAsync(batchRequest).ConfigureAwait(false);
        }
    }

    public async Task<bool> HasJobsInStatusAsync(string printerId, PrintJobStatus status)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        var gsi1Pk = $"PRINTER#{printerId}#STATUS#{status}";

        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :gsi1pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gsi1pk"] = new() { S = gsi1Pk }
            },
            Limit = 1,
            Select = Select.COUNT
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        return response.Count > 0;
    }

    public async Task<int> CountActiveJobsForPrinterAsync(string printerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        var totalCount = 0;

        foreach (var status in new[] { PrintJobStatus.Queued, PrintJobStatus.Processing })
        {
            var gsi1Pk = $"PRINTER#{printerId}#STATUS#{status}";

            var request = new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI1PK = :gsi1pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":gsi1pk"] = new() { S = gsi1Pk }
                },
                Select = Select.COUNT
            };

            var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);
            totalCount += response.Count ?? 0;
        }

        return totalCount;
    }

    /// <summary>Immediate conditional update — optimistic lock, not part of transaction.</summary>
    private async Task<bool> TryClaimJobAsync(PrintJob job)
    {
        try
        {
            var updateRequest = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [PartitionKey] = new() { S = $"PRINTER#{job.PrinterId.Value}" },
                    [SortKey] = new() { S = $"JOB#{job.Id.Value}" }
                },
                UpdateExpression = "SET #status = :newStatus, ProcessedAt = :processedAt, GSI1PK = :newGsi1Pk",
                ConditionExpression = "#status = :currentStatus",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = "Status"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":currentStatus"] = new() { S = PrintJobStatus.Queued.ToString() },
                    [":newStatus"] = new() { S = PrintJobStatus.Processing.ToString() },
                    [":processedAt"] = new() { S = DateTimeOffset.UtcNow.ToString("O") },
                    [":newGsi1Pk"] = new() { S = $"PRINTER#{job.PrinterId.Value}#STATUS#Processing" }
                }
            };

            await dynamoDbClient.UpdateItemAsync(updateRequest).ConfigureAwait(false);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            // Another process claimed this job
            return false;
        }
    }

    private static Dictionary<string, AttributeValue> MapToItem(PrintJob printJob)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            [PartitionKey] = new() { S = $"PRINTER#{printJob.PrinterId.Value}" },
            [SortKey] = new() { S = $"JOB#{printJob.Id.Value}" },
            [Gsi1PartitionKey] = new() { S = $"PRINTER#{printJob.PrinterId.Value}#STATUS#{printJob.Status}" },
            [Gsi1SortKey] = new() { S = printJob.CreatedAt.ToString("O", CultureInfo.InvariantCulture) },
            ["PrintJobId"] = new() { S = printJob.Id.Value },
            ["PrinterId"] = new() { S = printJob.PrinterId.Value },
            ["UserId"] = new() { S = printJob.UserId },
            ["StickerId"] = new() { S = printJob.StickerId },
            ["StickerUrl"] = new() { S = printJob.StickerUrl },
            ["Status"] = new() { S = printJob.Status.ToString() },
            ["CreatedAt"] = new() { S = printJob.CreatedAt.ToString("O", CultureInfo.InvariantCulture) }
        };

        if (!string.IsNullOrEmpty(printJob.TraceParent))
        {
            item["TraceParent"] = new() { S = printJob.TraceParent };
        }

        if (printJob.PropagationHeaders.Count > 0)
        {
            item["PropagationHeaders"] = new() { S = JsonSerializer.Serialize(printJob.PropagationHeaders) };
        }

        if (printJob.ProcessedAt.HasValue)
        {
            item["ProcessedAt"] = new() { S = printJob.ProcessedAt.Value.ToString("O", CultureInfo.InvariantCulture) };
        }

        if (printJob.CompletedAt.HasValue)
        {
            item["CompletedAt"] = new() { S = printJob.CompletedAt.Value.ToString("O", CultureInfo.InvariantCulture) };

            // Set TTL for completed/failed jobs (2 days after completion)
            var ttlTimestamp = printJob.CompletedAt.Value.Add(TtlDuration).ToUnixTimeSeconds();
            item["TTL"] = new() { N = ttlTimestamp.ToString(CultureInfo.InvariantCulture) };
        }

        if (!string.IsNullOrEmpty(printJob.FailureReason))
        {
            item["FailureReason"] = new() { S = printJob.FailureReason };
        }

        return item;
    }

    private static PrintJob MapToPrintJob(Dictionary<string, AttributeValue> item)
    {
        var printJobId = new PrintJobId(item["PrintJobId"].S);
        var printerId = new PrinterId(item["PrinterId"].S);
        var userId = item["UserId"].S;
        var stickerId = item["StickerId"].S;
        var stickerUrl = item["StickerUrl"].S;
        var status = Enum.Parse<PrintJobStatus>(item["Status"].S);
        var createdAt = DateTimeOffset.Parse(item["CreatedAt"].S, CultureInfo.InvariantCulture);

        DateTimeOffset? processedAt = null;
        if (item.TryGetValue("ProcessedAt", out var processedAtValue))
        {
            processedAt = DateTimeOffset.Parse(processedAtValue.S, CultureInfo.InvariantCulture);
        }

        DateTimeOffset? completedAt = null;
        if (item.TryGetValue("CompletedAt", out var completedAtValue))
        {
            completedAt = DateTimeOffset.Parse(completedAtValue.S, CultureInfo.InvariantCulture);
        }

        string? failureReason = null;
        if (item.TryGetValue("FailureReason", out var failureReasonValue))
        {
            failureReason = failureReasonValue.S;
        }

        string? traceParent = null;
        if (item.TryGetValue("TraceParent", out var traceParentValue))
        {
            traceParent = traceParentValue.S;
        }

        Dictionary<string, string>? propagationHeaders = null;
        if (item.TryGetValue("PropagationHeaders", out var propagationHeadersValue))
        {
            propagationHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(propagationHeadersValue.S);
        }

        return PrintJob.From(
            printJobId,
            printerId,
            userId,
            stickerId,
            stickerUrl,
            status,
            createdAt,
            processedAt,
            completedAt,
            failureReason,
            traceParent,
            propagationHeaders);
    }
}
