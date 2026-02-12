// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2026 Datadog, Inc.

using Amazon.EventBridge;
using AttributeValue = Amazon.Lambda.DynamoDBEvents.DynamoDBEvent.AttributeValue;
using StreamRecord = Amazon.Lambda.DynamoDBEvents.DynamoDBEvent.StreamRecord;
using Amazon.EventBridge.Model;
using Amazon.Lambda.DynamoDBEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.AWS;
using Stickerlandia.PrintService.Core.Observability;
using Stickerlandia.PrintService.Core.Outbox;
using Stickerlandia.PrintService.Lambda;

namespace Stickerlandia.PrintService.UnitTest.AwsTests;

public sealed class OutboxFunctionsTests : IDisposable
{
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly IOptions<AwsConfiguration> _awsConfiguration;
    private readonly ILogger<OutboxFunctions> _logger;
    private readonly OutboxFunctions _sut;
    private readonly PrintJobInstrumentation _instrumentation;

    public OutboxFunctionsTests()
    {
        _eventBridgeClient = A.Fake<IAmazonEventBridge>();
        _awsConfiguration = Options.Create(new AwsConfiguration
        {
            EventBusName = "test-event-bus",
            PrinterTableName = "Printers-test",
            PrintJobTableName = "PrintJobs-test"
        });
        _logger = A.Fake<ILogger<OutboxFunctions>>();
        _instrumentation = new PrintJobInstrumentation();
        var outboxProcessor = new OutboxProcessor(
            A.Fake<IServiceScopeFactory>(),
            A.Fake<ILogger<OutboxProcessor>>(),
            _instrumentation);

        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Returns(new PutEventsResponse { FailedEntryCount = 0, Entries = [] });

        _sut = new OutboxFunctions(_logger, outboxProcessor, _eventBridgeClient, _awsConfiguration);
    }

    public void Dispose()
    {
        _instrumentation.Dispose();
        (_eventBridgeClient as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task HandleStream_WithOutboxInsertRecord_PublishesToEventBridge()
    {
        var dynamoDbEvent = CreateDynamoDbEvent(
            CreateOutboxInsertRecord("evt-1", "printJobs.queued.v1", "{\"printJobId\":\"123\"}", "item-001"));

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().BeEmpty();
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(
                A<PutEventsRequest>.That.Matches(r =>
                    r.Entries.Count == 1 &&
                    r.Entries[0].DetailType == "printJobs.queued.v1" &&
                    r.Entries[0].EventBusName == "test-event-bus"),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleStream_WithNonInsertRecord_SkipsProcessing()
    {
        var record = CreateOutboxInsertRecord("evt-1", "printJobs.queued.v1", "{}", "item-001");
        record.EventName = "MODIFY";

        var dynamoDbEvent = CreateDynamoDbEvent(record);

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().BeEmpty();
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task HandleStream_WithNonOutboxRecord_SkipsProcessing()
    {
        var record = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventID = "evt-1",
            EventName = "INSERT",
            Dynamodb = new StreamRecord
            {
                NewImage = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new() { S = "PRINTER#TestEvent" },
                    ["SK"] = new() { S = "TestPrinter" }
                }
            }
        };

        var dynamoDbEvent = CreateDynamoDbEvent(record);

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().BeEmpty();
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task HandleStream_WithPublishFailure_ReportsAsBatchItemFailure()
    {
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .Throws(new AmazonEventBridgeException("Publish failed"));

        var dynamoDbEvent = CreateDynamoDbEvent(
            CreateOutboxInsertRecord("evt-fail", "printJobs.queued.v1", "{}", "item-fail"));

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().ContainSingle()
            .Which.ItemIdentifier.Should().Be("evt-fail");
    }

    [Fact]
    public async Task HandleStream_WithMixedRecords_OnlyProcessesOutboxInserts()
    {
        var outboxInsert = CreateOutboxInsertRecord("evt-1", "printJobs.queued.v1", "{}", "item-001");

        var modifyRecord = CreateOutboxInsertRecord("evt-2", "printJobs.queued.v1", "{}", "item-002");
        modifyRecord.EventName = "MODIFY";

        var nonOutboxInsert = new DynamoDBEvent.DynamodbStreamRecord
        {
            EventID = "evt-3",
            EventName = "INSERT",
            Dynamodb = new StreamRecord
            {
                NewImage = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new() { S = "PRINTER#TestEvent" },
                    ["SK"] = new() { S = "TestPrinter" }
                }
            }
        };

        var secondOutboxInsert = CreateOutboxInsertRecord("evt-4", "printers.registered.v1", "{}", "item-003");

        var dynamoDbEvent = CreateDynamoDbEvent(outboxInsert, modifyRecord, nonOutboxInsert, secondOutboxInsert);

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().BeEmpty();
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task HandleStream_UsesItemIdAsCloudEventId()
    {
        var dynamoDbEvent = CreateDynamoDbEvent(
            CreateOutboxInsertRecord("evt-1", "printJobs.queued.v1", "{\"data\":1}", "stable-item-id-123"));

        await _sut.HandleStream(dynamoDbEvent);

        A.CallTo(() => _eventBridgeClient.PutEventsAsync(
                A<PutEventsRequest>.That.Matches(r =>
                    r.Entries[0].Detail!.Contains("stable-item-id-123", StringComparison.Ordinal)),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleStream_WithPartialFailure_ReportsOnlyFailedRecords()
    {
        // First call succeeds, second call fails
        A.CallTo(() => _eventBridgeClient.PutEventsAsync(A<PutEventsRequest>._, A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new PutEventsResponse { FailedEntryCount = 0, Entries = [] },
                new PutEventsResponse
                {
                    FailedEntryCount = 1,
                    Entries = [new PutEventsResultEntry { ErrorCode = "InternalError", ErrorMessage = "Boom" }]
                });

        var record1 = CreateOutboxInsertRecord("evt-1", "printJobs.queued.v1", "{}", "item-001");
        var record2 = CreateOutboxInsertRecord("evt-2", "printJobs.completed.v1", "{}", "item-002");

        var dynamoDbEvent = CreateDynamoDbEvent(record1, record2);

        var response = await _sut.HandleStream(dynamoDbEvent);

        response.BatchItemFailures.Should().ContainSingle()
            .Which.ItemIdentifier.Should().Be("evt-2");
    }

    private static DynamoDBEvent CreateDynamoDbEvent(params DynamoDBEvent.DynamodbStreamRecord[] records)
    {
        return new DynamoDBEvent { Records = [.. records] };
    }

    private static DynamoDBEvent.DynamodbStreamRecord CreateOutboxInsertRecord(
        string eventId, string eventType, string eventData, string itemId)
    {
        return new DynamoDBEvent.DynamodbStreamRecord
        {
            EventID = eventId,
            EventName = "INSERT",
            Dynamodb = new StreamRecord
            {
                NewImage = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new() { S = $"{OutboxItemAttributes.PkPrefix}{itemId}" },
                    ["SK"] = new() { S = $"{OutboxItemAttributes.SkPrefix}{eventType}" },
                    [OutboxItemAttributes.EventType] = new() { S = eventType },
                    [OutboxItemAttributes.EventData] = new() { S = eventData },
                    [OutboxItemAttributes.ItemId] = new() { S = itemId },
                    [OutboxItemAttributes.ItemType] = new() { S = OutboxItemAttributes.ItemTypeValue },
                    [OutboxItemAttributes.TraceId] = new() { S = "00-abc123-def456-01" }
                }
            }
        };
    }
}
