using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stickerlandia.PrintService.AWS;
using Stickerlandia.PrintService.Core;
using Stickerlandia.PrintService.Core.DeletePrinter;
using Stickerlandia.PrintService.Core.PrintJobs;
using Stickerlandia.PrintService.Core.RegisterPrinter;

namespace Stickerlandia.PrintService.UnitTest.AwsTests;

public class DynamoDbOutboxTests : IAsyncDisposable
{
    private readonly DynamoDbWriteTransaction _transaction;
    private readonly DynamoDbOutbox _sut;
    private readonly AwsConfiguration _config;

    public DynamoDbOutboxTests()
    {
        var dynamoDb = A.Fake<IAmazonDynamoDB>();
        var transactionLogger = A.Fake<ILogger<DynamoDbWriteTransaction>>();
        _transaction = new DynamoDbWriteTransaction(dynamoDb, transactionLogger);

        _config = new AwsConfiguration
        {
            PrinterTableName = "Printers-test",
            PrintJobTableName = "PrintJobs-test",
            EventBusName = "test-bus"
        };
        var options = A.Fake<IOptions<AwsConfiguration>>();
        A.CallTo(() => options.Value).Returns(_config);

        var logger = A.Fake<ILogger<DynamoDbOutbox>>();

        _sut = new DynamoDbOutbox(_transaction, options, logger);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _transaction.DisposeAsync();
    }

    [Fact]
    public async Task StoreEventFor_PrinterRegisteredEvent_RegistersPutOnPrinterTable()
    {
        var evt = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER1" };

        await _sut.StoreEventFor(evt);

        // Transaction should have 1 buffered operation (the outbox item)
        // Commit will use PutItemAsync since it's a single item
        // We verify indirectly by checking the transaction has operations
        _transaction.OperationCount.Should().Be(1);
    }

    [Fact]
    public async Task StoreEventFor_PrintJobQueuedEvent_RegistersPutOnPrintJobTable()
    {
        var printJob = PrintJob.Create(
            new PrinterId("TESTEVENT-TESTPRINTER"),
            "user-1",
            "sticker-1",
            "https://example.com/sticker.png");
        var evt = new PrintJobQueuedEvent(printJob);

        await _sut.StoreEventFor(evt);

        _transaction.OperationCount.Should().Be(1);
    }

    [Fact]
    public async Task StoreEventFor_SetsCorrectPkSkPattern()
    {
        var evt = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER1" };

        await _sut.StoreEventFor(evt);

        var item = _transaction.GetBufferedItem(0);
        item["PK"].S.Should().StartWith(OutboxItemAttributes.PkPrefix);
        item["SK"].S.Should().Be($"{OutboxItemAttributes.SkPrefix}{evt.EventName}");
    }

    [Fact]
    public async Task StoreEventFor_SetsTtlTo7Days()
    {
        var evt = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER1" };

        await _sut.StoreEventFor(evt);

        var item = _transaction.GetBufferedItem(0);
        var ttl = long.Parse(item[OutboxItemAttributes.Ttl].N, System.Globalization.CultureInfo.InvariantCulture);
        var eventTime = DateTime.Parse(item[OutboxItemAttributes.EventTime].S, System.Globalization.CultureInfo.InvariantCulture);
        var expectedTtl = new DateTimeOffset(eventTime).ToUnixTimeSeconds() + (long)OutboxItemAttributes.TtlDuration.TotalSeconds;

        ttl.Should().Be(expectedTtl);
    }

    [Fact]
    public async Task StoreEventFor_SetsEventTypeAndEventData()
    {
        var evt = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER1" };

        await _sut.StoreEventFor(evt);

        var item = _transaction.GetBufferedItem(0);
        item[OutboxItemAttributes.EventType].S.Should().Be("printers.registered.v1");
        item[OutboxItemAttributes.EventData].S.Should().Contain("TESTEVENT-PRINTER1");
        item[OutboxItemAttributes.ItemType].S.Should().Be(OutboxItemAttributes.ItemTypeValue);
    }

    [Fact]
    public async Task StoreEventFor_SetsStableItemId()
    {
        var evt = new PrinterRegisteredEvent { PrinterId = "TESTEVENT-PRINTER1" };

        await _sut.StoreEventFor(evt);

        var item = _transaction.GetBufferedItem(0);
        item[OutboxItemAttributes.ItemId].S.Should().NotBeNullOrEmpty();
        // ItemId should be a valid GUID
        Guid.TryParse(item[OutboxItemAttributes.ItemId].S, out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetUnprocessedItemsAsync_ReturnsEmptyList()
    {
        var result = await _sut.GetUnprocessedItemsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOutboxItem_DoesNotThrow()
    {
        var outboxItem = new Core.Outbox.OutboxItem
        {
            EventType = "test",
            EventData = "{}",
            Processed = true
        };

        var act = async () => await _sut.UpdateOutboxItem(outboxItem);

        await act.Should().NotThrowAsync();
    }
}
