using System.Diagnostics;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.Outbox;

namespace Stickerlandia.PrintService.AWS;

public sealed partial class DynamoDbOutbox : IOutbox
{
    private readonly DynamoDbWriteTransaction _transaction;
    private readonly AwsConfiguration _config;
    private readonly ILogger<DynamoDbOutbox> _logger;

    public DynamoDbOutbox(
        DynamoDbWriteTransaction transaction,
        IOptions<AwsConfiguration> options,
        ILogger<DynamoDbOutbox> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _transaction = transaction;
        _config = options.Value;
        _logger = logger;
    }

    public Task StoreEventFor(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var itemId = Guid.NewGuid().ToString();
        var eventTime = DateTime.UtcNow;
        var ttlEpoch = new DateTimeOffset(eventTime).ToUnixTimeSeconds()
                       + (long)OutboxItemAttributes.TtlDuration.TotalSeconds;

        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new($"{OutboxItemAttributes.PkPrefix}{itemId}"),
            ["SK"] = new($"{OutboxItemAttributes.SkPrefix}{domainEvent.EventName}"),
            [OutboxItemAttributes.ItemType] = new(OutboxItemAttributes.ItemTypeValue),
            [OutboxItemAttributes.EventType] = new(domainEvent.EventName),
            [OutboxItemAttributes.EventData] = new(domainEvent.ToJsonString()),
            [OutboxItemAttributes.EventTime] = new(eventTime.ToString("O", CultureInfo.InvariantCulture)),
            [OutboxItemAttributes.Ttl] = new() { N = ttlEpoch.ToString(CultureInfo.InvariantCulture) },
            [OutboxItemAttributes.ItemId] = new(itemId)
        };

        var traceId = Activity.Current?.Id;
        if (traceId != null)
        {
            item[OutboxItemAttributes.TraceId] = new AttributeValue(traceId);
        }

        var tableName = ResolveTableName(domainEvent.EventName);

        _transaction.AddPut(tableName, item);

        LogOutboxItemBuffered(_logger, domainEvent.EventName, itemId, tableName);

        return Task.CompletedTask;
    }

    public Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100) =>
        Task.FromResult(new List<OutboxItem>());

    public Task UpdateOutboxItem(OutboxItem outboxItem) => Task.CompletedTask;

    private string ResolveTableName(string eventName) =>
        eventName.StartsWith("printers.", StringComparison.Ordinal)
            ? _config.PrinterTableName
            : _config.PrintJobTableName;

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Buffered outbox item for event {EventName} with ItemId {ItemId} on table {TableName}")]
    private static partial void LogOutboxItemBuffered(ILogger logger, string eventName, string itemId, string tableName);
}
