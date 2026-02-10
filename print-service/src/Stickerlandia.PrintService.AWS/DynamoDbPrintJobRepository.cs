// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.PrintJobs;

namespace Stickerlandia.PrintService.AWS;

public class DynamoDbPrintJobRepository(
    IAmazonDynamoDB dynamoDbClient,
    IOptions<AwsConfiguration> configuration) : IPrintJobRepository
{
    private const string PartitionKey = "PK";
    private const string SortKey = "SK";
    private const string Gsi1PartitionKey = "GSI1PK";
    private const string Gsi1SortKey = "GSI1SK";

    private readonly string _tableName = configuration.Value.PrintJobTableName;

    // TTL duration for completed/failed jobs: 2 days
    private static readonly TimeSpan TtlDuration = TimeSpan.FromDays(2);

    public async Task<PrintJob> AddAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var item = MapToItem(printJob);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);

        return printJob;
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

    public async Task UpdateAsync(PrintJob printJob)
    {
        ArgumentNullException.ThrowIfNull(printJob);

        var item = MapToItem(printJob);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
    }

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
            failureReason);
    }
}
