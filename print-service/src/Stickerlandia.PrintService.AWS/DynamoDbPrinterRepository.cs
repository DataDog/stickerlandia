// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.AWS;

public class DynamoDbPrinterRepository(
    IAmazonDynamoDB dynamoDbClient,
    IOptions<AwsConfiguration> configuration) : IPrinterRepository
{
    private const string PartitionKey = "PK";
    private const string SortKey = "SK";
    private const string Gsi1PartitionKey = "GSI1PK";

    private readonly string _tableName = configuration.Value.PrinterTableName;

    public async Task AddPrinterAsync(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printer.Id);

        var item = new Dictionary<string, AttributeValue>
        {
            [PartitionKey] = new() { S = printer.EventName },
            [SortKey] = new() { S = printer.Id.Value },
            [Gsi1PartitionKey] = new() { S = printer.Key },
            ["EventName"] = new() { S = printer.EventName },
            ["PrinterName"] = new() { S = printer.PrinterName },
            ["Key"] = new() { S = printer.Key }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
    }

    public async Task<Printer?> GetPrinterByIdAsync(Guid printerId)
    {
        var printerIdString = printerId.ToString();

        var request = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "SK = :printerId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":printerId"] = new() { S = printerIdString }
            }
        };

        var response = await dynamoDbClient.ScanAsync(request).ConfigureAwait(false);

        if (response.Items.Count == 0)
        {
            return null;
        }

        return MapToPrinter(response.Items[0]);
    }

    public async Task<List<Printer>> GetPrintersForEventAsync(string eventName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :eventName",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":eventName"] = new() { S = eventName }
            }
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        return response.Items.Select(MapToPrinter).ToList();
    }

    public async Task<bool> PrinterExistsAsync(string eventName, string printerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(printerName);

        var printerId = $"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}";

        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [PartitionKey] = new() { S = eventName },
                [SortKey] = new() { S = printerId }
            },
            ProjectionExpression = "PK"
        };

        var response = await dynamoDbClient.GetItemAsync(request).ConfigureAwait(false);

        return response.IsItemSet;
    }

    public async Task<Printer?> GetPrinterAsync(string eventName, string printerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(printerName);

        var printerId = $"{eventName.ToUpperInvariant()}-{printerName.ToUpperInvariant()}";

        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [PartitionKey] = new() { S = eventName },
                [SortKey] = new() { S = printerId }
            }
        };

        var response = await dynamoDbClient.GetItemAsync(request).ConfigureAwait(false);

        if (!response.IsItemSet)
        {
            return null;
        }

        return MapToPrinter(response.Item);
    }

    public async Task<Printer?> GetPrinterByKeyAsync(string apiKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        // Query GSI1 where GSI1PK = apiKey (the printer's API key)
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :key",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":key"] = new() { S = apiKey }
            },
            Limit = 1
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        if (response.Items.Count == 0)
        {
            return null;
        }

        return MapToPrinter(response.Items[0]);
    }

    public async Task UpdateHeartbeatAsync(string printerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(printerId);

        // We need to find the event name from the printerId first
        // The printerId format is "EVENTNAME-PRINTERNAME"
        var request = new ScanRequest
        {
            TableName = _tableName,
            FilterExpression = "SK = :printerId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":printerId"] = new() { S = printerId }
            }
        };

        var response = await dynamoDbClient.ScanAsync(request).ConfigureAwait(false);

        if (response.Items.Count == 0)
        {
            return;
        }

        var item = response.Items[0];
        var eventName = item[PartitionKey].S;

        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [PartitionKey] = new() { S = eventName },
                [SortKey] = new() { S = printerId }
            },
            UpdateExpression = "SET LastHeartbeat = :heartbeat",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":heartbeat"] = new() { S = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) }
            }
        };

        await dynamoDbClient.UpdateItemAsync(updateRequest).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printer.Id);

        var item = new Dictionary<string, AttributeValue>
        {
            [PartitionKey] = new() { S = printer.EventName },
            [SortKey] = new() { S = printer.Id.Value },
            [Gsi1PartitionKey] = new() { S = printer.Key },
            ["EventName"] = new() { S = printer.EventName },
            ["PrinterName"] = new() { S = printer.PrinterName },
            ["Key"] = new() { S = printer.Key }
        };

        if (printer.LastHeartbeat.HasValue)
        {
            item["LastHeartbeat"] = new() { S = printer.LastHeartbeat.Value.ToString("O", CultureInfo.InvariantCulture) };
        }

        if (printer.LastJobProcessed.HasValue)
        {
            item["LastJobProcessed"] = new() { S = printer.LastJobProcessed.Value.ToString("O", CultureInfo.InvariantCulture) };
        }

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
    }

    private static Printer MapToPrinter(Dictionary<string, AttributeValue> item)
    {
        DateTimeOffset? lastHeartbeat = null;
        if (item.TryGetValue("LastHeartbeat", out var lastHeartbeatValue))
        {
            lastHeartbeat = DateTimeOffset.Parse(lastHeartbeatValue.S, CultureInfo.InvariantCulture);
        }

        DateTimeOffset? lastJobProcessed = null;
        if (item.TryGetValue("LastJobProcessed", out var lastJobProcessedValue))
        {
            lastJobProcessed = DateTimeOffset.Parse(lastJobProcessedValue.S, CultureInfo.InvariantCulture);
        }

        return Printer.From(
            new PrinterId(item["SK"].S),
            item["EventName"].S,
            item["PrinterName"].S,
            item["Key"].S,
            lastHeartbeat,
            lastJobProcessed);
    }
}
