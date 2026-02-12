using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Stickerlandia.PrintService.Core;

namespace Stickerlandia.PrintService.AWS;

public sealed partial class DynamoDbWriteTransaction : IUnitOfWork
{
    private const int MaxTransactItems = 100;

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<DynamoDbWriteTransaction> _logger;
    private readonly List<TransactWriteItem> _operations = [];
    private bool _committed;
    private bool _faulted;

    public DynamoDbWriteTransaction(IAmazonDynamoDB dynamoDb, ILogger<DynamoDbWriteTransaction> logger)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
    }

    public void AddPut(string tableName, Dictionary<string, AttributeValue> item)
    {
        _operations.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = tableName,
                Item = item
            }
        });
    }

    public void AddDelete(string tableName, Dictionary<string, AttributeValue> key)
    {
        _operations.Add(new TransactWriteItem
        {
            Delete = new Delete
            {
                TableName = tableName,
                Key = key
            }
        });
    }

    internal int OperationCount => _operations.Count;

    internal Dictionary<string, AttributeValue> GetBufferedItem(int index) =>
        _operations[index].Put?.Item
        ?? _operations[index].Delete?.Key
        ?? throw new InvalidOperationException("Operation has neither Put nor Delete.");

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_committed)
        {
            return;
        }

        switch (_operations.Count)
        {
            case 0:
                _committed = true;
                return;

            case 1:
                var single = _operations[0];
                if (single.Put != null)
                {
                    await _dynamoDb.PutItemAsync(
                        new PutItemRequest
                        {
                            TableName = single.Put.TableName,
                            Item = single.Put.Item
                        },
                        cancellationToken);
                }
                else if (single.Delete != null)
                {
                    await _dynamoDb.DeleteItemAsync(
                        new DeleteItemRequest
                        {
                            TableName = single.Delete.TableName,
                            Key = single.Delete.Key
                        },
                        cancellationToken);
                }

                _committed = true;
                return;

            default:
                if (_operations.Count > MaxTransactItems)
                {
                    throw new InvalidOperationException(
                        $"DynamoDB TransactWriteItems supports a maximum of {MaxTransactItems} items, " +
                        $"but {_operations.Count} were buffered.");
                }

                await _dynamoDb.TransactWriteItemsAsync(
                    new TransactWriteItemsRequest
                    {
                        TransactItems = _operations
                    },
                    cancellationToken);
                _committed = true;
                return;
        }
    }

    public void MarkFaulted() => _faulted = true;

    public ValueTask DisposeAsync()
    {
        if (_operations.Count > 0 && !_committed)
        {
            if (_faulted)
            {
                LogUncommittedOperationsExpected(_logger, _operations.Count);
            }
            else
            {
                LogUncommittedOperations(_logger, _operations.Count);
            }
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "DynamoDbWriteTransaction disposed with {Count} uncommitted operations. CommitAsync() was never called. Operations were LOST.")]
    private static partial void LogUncommittedOperations(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "DynamoDbWriteTransaction disposed with {Count} uncommitted operations after handler exception. Operations were not persisted.")]
    private static partial void LogUncommittedOperationsExpected(ILogger logger, int count);
}
