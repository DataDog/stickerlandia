using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.AWS;

namespace Stickerlandia.PrintService.UnitTest.AwsTests;

public class DynamoDbWriteTransactionTests
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<DynamoDbWriteTransaction> _logger;

    public DynamoDbWriteTransactionTests()
    {
        _dynamoDb = A.Fake<IAmazonDynamoDB>();
        _logger = A.Fake<ILogger<DynamoDbWriteTransaction>>();
        A.CallTo(() => _logger.IsEnabled(A<LogLevel>._)).Returns(true);
    }

    [Fact]
    public async Task CommitAsync_WithNoOperations_DoesNotCallDynamoDB()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);

        await sut.CommitAsync();

        A.CallTo(_dynamoDb).MustNotHaveHappened();
    }

    [Fact]
    public async Task CommitAsync_WithSinglePut_UsesPutItemAsync()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("TEST"),
            ["SK"] = new("ITEM#1")
        };

        A.CallTo(() => _dynamoDb.PutItemAsync(A<PutItemRequest>._, A<CancellationToken>._))
            .Returns(new PutItemResponse());

        sut.AddPut("TestTable", item);
        await sut.CommitAsync();

        A.CallTo(() => _dynamoDb.PutItemAsync(
                A<PutItemRequest>.That.Matches(r => r.TableName == "TestTable" && r.Item["PK"].S == "TEST"),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _dynamoDb.TransactWriteItemsAsync(A<TransactWriteItemsRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CommitAsync_WithMultiplePuts_UsesTransactWriteItemsAsync()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var item1 = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("ENTITY#1"),
            ["SK"] = new("DATA")
        };
        var item2 = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("OUTBOX#1"),
            ["SK"] = new("EVENT#test")
        };

        A.CallTo(() => _dynamoDb.TransactWriteItemsAsync(A<TransactWriteItemsRequest>._, A<CancellationToken>._))
            .Returns(new TransactWriteItemsResponse());

        sut.AddPut("TestTable", item1);
        sut.AddPut("TestTable", item2);
        await sut.CommitAsync();

        A.CallTo(() => _dynamoDb.TransactWriteItemsAsync(
                A<TransactWriteItemsRequest>.That.Matches(r => r.TransactItems.Count == 2),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _dynamoDb.PutItemAsync(A<PutItemRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CommitAsync_CalledTwice_SecondCallIsNoOp()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("TEST"),
            ["SK"] = new("ITEM#1")
        };

        A.CallTo(() => _dynamoDb.PutItemAsync(A<PutItemRequest>._, A<CancellationToken>._))
            .Returns(new PutItemResponse());

        sut.AddPut("TestTable", item);
        await sut.CommitAsync();
        await sut.CommitAsync();

        A.CallTo(() => _dynamoDb.PutItemAsync(A<PutItemRequest>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DisposeAsync_WithUncommittedOperations_LogsCriticalWarning()
    {
        var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("TEST"),
            ["SK"] = new("ITEM#1")
        };

        sut.AddPut("TestTable", item);
        await sut.DisposeAsync();

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log"
                           && call.GetArgument<LogLevel>(0) == LogLevel.Critical)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DisposeAsync_AfterCommit_DoesNotLogWarning()
    {
        var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("TEST"),
            ["SK"] = new("ITEM#1")
        };

        A.CallTo(() => _dynamoDb.PutItemAsync(A<PutItemRequest>._, A<CancellationToken>._))
            .Returns(new PutItemResponse());

        sut.AddPut("TestTable", item);
        await sut.CommitAsync();
        await sut.DisposeAsync();

        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log"
                           && call.GetArgument<LogLevel>(0) == LogLevel.Critical)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task CommitAsync_ExceedsMaxItems_ThrowsInvalidOperationException()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);

        for (var i = 0; i < 101; i++)
        {
            sut.AddPut("TestTable", new Dictionary<string, AttributeValue>
            {
                ["PK"] = new($"ITEM#{i}"),
                ["SK"] = new("DATA")
            });
        }

        var act = async () => await sut.CommitAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CommitAsync_WithPutAndDelete_UsesTransactWriteItemsAsync()
    {
        await using var sut = new DynamoDbWriteTransaction(_dynamoDb, _logger);
        var putItem = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("OUTBOX#1"),
            ["SK"] = new("EVENT#test")
        };
        var deleteKey = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new("PRINTER#1"),
            ["SK"] = new("DATA")
        };

        A.CallTo(() => _dynamoDb.TransactWriteItemsAsync(A<TransactWriteItemsRequest>._, A<CancellationToken>._))
            .Returns(new TransactWriteItemsResponse());

        sut.AddPut("TestTable", putItem);
        sut.AddDelete("TestTable", deleteKey);
        await sut.CommitAsync();

        A.CallTo(() => _dynamoDb.TransactWriteItemsAsync(
                A<TransactWriteItemsRequest>.That.Matches(r =>
                    r.TransactItems.Count == 2
                    && r.TransactItems[0].Put != null
                    && r.TransactItems[1].Delete != null),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
