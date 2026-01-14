// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.AWS;

/// <summary>
/// DynamoDB implementation of printer key validation using GSI1 index.
/// </summary>
public class DynamoDbPrinterKeyValidator(
    IAmazonDynamoDB dynamoDbClient,
    IOptions<AwsConfiguration> configuration) : IPrinterKeyValidator
{
    private readonly string _tableName = configuration.Value.PrinterTableName;

    public async Task<Printer?> ValidateKeyAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // Query GSI1 where GSI1PK = key (the printer's API key)
        var request = new QueryRequest
        {
            TableName = _tableName,
            IndexName = "GSI1",
            KeyConditionExpression = "GSI1PK = :key",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":key"] = new() { S = key }
            },
            Limit = 1
        };

        var response = await dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        if (response.Items.Count == 0)
        {
            return null;
        }

        var item = response.Items[0];

        return Printer.From(
            new PrinterId(item["SK"].S),
            item["EventName"].S,
            item["PrinterName"].S,
            item["Key"].S);
    }
}
