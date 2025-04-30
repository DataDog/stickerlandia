using Microsoft.Azure.Cosmos;
using Stickerlandia.UserManagement.Core;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Azure;

internal record CosmosDbUserAccount(
    string id,
    string emailAddress,
    string PasswordHash,
    string FirstName,
    string LastName,
    int ClaimedStickerCount,
    DateTime DateCreated,
    AccountTier AccountTier,
    AccountType AccountType)
{
    public string DocumentType => "UserAccount";

    // partition the IoT device by user id
    internal static string ContainerPartitionKey = "/emailAddress";
}

internal class CosmosDbOutboxItem : OutboxItem
{
    // Use the same properties as OutboxItem
    public string id
    {
        get
        {
            return ItemId;
        }
        set
        {
            ItemId = value;
        }
    }
    
    public string emailAddress
    {
        get
        {
            return EmailAddress;
        }
        set
        {
            EmailAddress = value;
        }
    }

    // Add discriminator field
    public string DocumentType => "OutboxItem";
}

public class CosmosDbUserRepository : IUserAccountRepository, IOutbox
{
    private readonly ILogger<CosmosDbUserRepository> _logger;
    private readonly Container _usersContainer;
    private readonly bool _isEmulator;
    
    private const string DatabaseId = "Stickerlandia";
    private const string ContainerId = "Users";
    private readonly ConcurrentDictionary<string, UserAccount> _userCache = new();

    public CosmosDbUserRepository(CosmosClient cosmosClient, ILogger<CosmosDbUserRepository> logger)
    {
        _logger = logger;
        _usersContainer = cosmosClient.GetContainer(DatabaseId, ContainerId);
        _isEmulator = cosmosClient.Endpoint.Host.Contains("localhost");
    }

    public async Task<UserAccount> CreateAccount(UserAccount userAccount)
    {
        try
        {
            // The CosmosDB V-Next emulator does not support batch operations, dynamically switch if running in emulator
            if (_isEmulator)
            {
                await CreateWithSeparateInserts(userAccount);
            }
            else
            {
                await CreateWithTransaction(userAccount);
            }
            
            return userAccount;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new UserExistsException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account");
            throw new DatabaseFailureException("Failed to create account", ex);
        }
    }

    private async Task CreateWithSeparateInserts(UserAccount userAccount)
    {
        await _usersContainer.CreateItemAsync(new CosmosDbUserAccount(
            userAccount.Id,
            userAccount.EmailAddress,
            userAccount.Password,
            userAccount.FirstName,
            userAccount.LastName,
            userAccount.ClaimedStickerCount,
            userAccount.DateCreated,
            userAccount.AccountTier,
            userAccount.AccountType));

        foreach (var evt in userAccount.DomainEvents)
        {
            var outboxItem = new CosmosDbOutboxItem
            {
                EventType = evt.EventName,
                EventData = evt.ToJsonString(),
                EmailAddress = userAccount.EmailAddress // Use same partition key as user
            };

            await _usersContainer.CreateItemAsync(outboxItem);
        }
    }

    private async Task CreateWithTransaction(UserAccount userAccount)
    {
        var batch = _usersContainer.CreateTransactionalBatch(new PartitionKey(userAccount.EmailAddress));

        batch.CreateItem(new CosmosDbUserAccount(
            userAccount.Id,
            userAccount.EmailAddress,
            userAccount.Password,
            userAccount.FirstName,
            userAccount.LastName,
            userAccount.ClaimedStickerCount,
            userAccount.DateCreated,
            userAccount.AccountTier,
            userAccount.AccountType));

        foreach (var evt in userAccount.DomainEvents)
        {
            var outboxItem = new CosmosDbOutboxItem
            {
                EventType = evt.EventName,
                EventData = evt.ToJsonString(),
                EmailAddress = userAccount.EmailAddress // Use same partition key as user
            };

            batch.CreateItem(outboxItem);
        }

        using var response = await batch.ExecuteAsync();

        if (response.IsSuccessStatusCode) return;

        throw new DatabaseFailureException($"Failed to create account with outbox event: {response.StatusCode}");
    }

    public async Task UpdateAccount(UserAccount userAccount)
    {
        try
        {
            if (_isEmulator)
            {
                await UpdateWithSeparateOperations(userAccount);
            }
            else
            {
                await UpdateWithTransaction(userAccount);
            }
            
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new DatabaseFailureException("User account not found", ex);
        }
    }
    
    private async Task UpdateWithSeparateOperations(UserAccount userAccount)
    {
        await _usersContainer.ReplaceItemAsync(new CosmosDbUserAccount(
            userAccount.Id,
            userAccount.EmailAddress,
            userAccount.Password,
            userAccount.FirstName,
            userAccount.LastName,
            userAccount.ClaimedStickerCount,
            userAccount.DateCreated,
            userAccount.AccountTier,
            userAccount.AccountType), userAccount.Id);

        foreach (var evt in userAccount.DomainEvents)
        {
            var outboxItem = new CosmosDbOutboxItem
            {
                EventType = evt.EventName,
                EventData = evt.ToJsonString(),
                EmailAddress = userAccount.EmailAddress // Use same partition key as user
            };

            await _usersContainer.CreateItemAsync(outboxItem);
        }
    }

    private async Task UpdateWithTransaction(UserAccount userAccount)
    {
        var batch = _usersContainer.CreateTransactionalBatch(new PartitionKey(userAccount.EmailAddress));

        // Replace the existing user account document
        batch.ReplaceItem(userAccount.Id, new CosmosDbUserAccount(
            userAccount.Id,
            userAccount.EmailAddress,
            userAccount.Password,
            userAccount.FirstName,
            userAccount.LastName,
            userAccount.ClaimedStickerCount,
            userAccount.DateCreated,
            userAccount.AccountTier,
            userAccount.AccountType));

        // Add outbox items for domain events
        foreach (var evt in userAccount.DomainEvents)
        {
            var outboxItem = new CosmosDbOutboxItem
            {
                EventType = evt.EventName,
                EventData = evt.ToJsonString(),
                EmailAddress = userAccount.EmailAddress // Use same partition key as user
            };

            batch.CreateItem(outboxItem);
        }

        using var response = await batch.ExecuteAsync();

        if (!response.IsSuccessStatusCode)
            throw new DatabaseFailureException($"Failed to update account with outbox event: {response.StatusCode} - {response.ErrorMessage}");
    }

    public async Task<UserAccount> ValidateCredentials(string emailAddress, string password)
    {
        try
        {
            // Query for user by email address
            var passwordHash = UserAccount.HashPassword(password);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.emailAddress = @email AND c.password = @password")
                .WithParameter("@email", emailAddress)
                .WithParameter("@password", passwordHash);

            using var iterator = _usersContainer.GetItemQueryIterator<CosmosDbUserAccount>(query);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var user = response.FirstOrDefault();

                if (user != null)
                    return UserAccount.From(user.id, user.emailAddress, user.PasswordHash, user.FirstName,
                        user.LastName,
                        user.ClaimedStickerCount, user.DateCreated,
                        user.AccountTier, user.AccountType);
            }

            throw new LoginFailedException();
        }
        catch (CosmosException)
        {
            throw new LoginFailedException();
        }
    }

    public async Task SeedInitialUser()
    {
        try
        {
            // Check if any users exist
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            using var countIterator = _usersContainer.GetItemQueryIterator<int>(countQuery);

            if (countIterator.HasMoreResults)
            {
                var response = await countIterator.ReadNextAsync();
                var count = response.FirstOrDefault();

                if (count > 0)
                    // Users already exist, no seeding needed
                    return;
            }

            // Create a default admin user
            var adminUser = await UserAccount.CreateAsync(
                "admin@stickerlandia.com",
                "Admin123!",
                "Admin",
                "User",
                AccountType.Staff);

            await CreateAccount(adminUser);
        }
        catch (CosmosException ex)
        {
            throw new DatabaseFailureException("Failed to seed initial user", ex);
        }
    }

    public async Task<UserAccount?> GetAccountByIdAsync(string accountId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", accountId);

            using var iterator = _usersContainer.GetItemQueryIterator<CosmosDbUserAccount>(query);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var user = response.FirstOrDefault();

                if (user != null)
                {
                    var account = UserAccount.From(user.id, user.emailAddress, user.PasswordHash,
                        user.FirstName,
                        user.LastName,
                        user.ClaimedStickerCount,
                        user.DateCreated,
                        user.AccountTier, user.AccountType);
                    // Cache the result
                    _userCache.TryAdd(accountId, account);
                    return account;
                }
            }

            return null;
        }
        catch (CosmosException)
        {
            return null;
        }
    }

    public async Task<UserAccount?> GetAccountByEmailAsync(string emailAddress)
    {
        // Check cache first (iterating because we cache by ID but are searching by email)
        var cachedUser =
            _userCache.Values.FirstOrDefault(u =>
                u.EmailAddress.Equals(emailAddress, StringComparison.OrdinalIgnoreCase));
        if (cachedUser != null) return cachedUser;

        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.emailAddress = @email")
                .WithParameter("@email", emailAddress);

            using var iterator = _usersContainer.GetItemQueryIterator<CosmosDbUserAccount>(query);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var user = response.FirstOrDefault();

                if (user != null)
                {
                    var account = UserAccount.From(user.id, user.emailAddress, user.PasswordHash, user.FirstName,
                        user.LastName,
                        user.ClaimedStickerCount, user.DateCreated,
                        user.AccountTier, user.AccountType);
                    // Cache the result
                    _userCache.TryAdd(account.Id, account);
                    return account;
                }
            }

            return null;
        }
        catch (CosmosException)
        {
            return null;
        }
    }

    public async Task<bool> DoesEmailExistAsync(string emailAddress)
    {
        // Check cache first
        if (_userCache.Values.Any(u => u.EmailAddress.Equals(emailAddress, StringComparison.OrdinalIgnoreCase)))
            return true;

        try
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.emailAddress = @email")
                .WithParameter("@email", emailAddress);

            using var iterator = _usersContainer.GetItemQueryIterator<int>(query);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                var count = response.FirstOrDefault();

                return count > 0;
            }

            return false;
        }
        catch (CosmosException)
        {
            return false;
        }
    }

    public async Task PreloadFrequentUsersAsync()
    {
        try
        {
            // Query for frequently active users (e.g., staff accounts)
            var query = new QueryDefinition("SELECT * FROM c WHERE c.accountType = 'Staff'");
            using var iterator = _usersContainer.GetItemQueryIterator<CosmosDbUserAccount>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var user in response)
                {
                    var account = UserAccount.From(user.id, user.emailAddress, user.PasswordHash, user.FirstName,
                        user.LastName, 
                        user.ClaimedStickerCount, DateTime.UtcNow,
                        user.AccountTier, user.AccountType);
                    _userCache.TryAdd(account.Id, account);
                }

                ;
            }
        }
        catch (CosmosException ex)
        {
            throw new DatabaseFailureException("Failed to preload frequent users", ex);
        }
    }

    public Task InvalidateCacheForUserAsync(string accountId)
    {
        _userCache.TryRemove(accountId, out _);
        return Task.CompletedTask;
    }

    public async Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.Processed = false AND c.Failed = false")
            .WithParameter("@maxCount", maxCount);

        var unprocessedItems = new List<OutboxItem>();

        using var iterator = _usersContainer.GetItemQueryIterator<CosmosDbOutboxItem>(query);

        while (iterator.HasMoreResults && unprocessedItems.Count < maxCount)
        {
            var response = await iterator.ReadNextAsync();
            unprocessedItems.AddRange(response.Select(item => new OutboxItem
            {
                ItemId = item.ItemId,
                EventType = item.EventType,
                EventData = item.EventData,
                EventTime = item.EventTime,
                Processed = item.Processed,
                Failed = item.Failed,
                FailureReason = item.FailureReason,
                TraceId = item.TraceId,
                EmailAddress = item.EmailAddress,
            }));
        }

        return unprocessedItems;
    }

    public async Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        try
        {
            var cosmosOutboxItem = new CosmosDbOutboxItem
            {
                ItemId = outboxItem.ItemId,
                EventType = outboxItem.EventType,
                EventData = outboxItem.EventData,
                EventTime = outboxItem.EventTime,
                Processed = outboxItem.Processed,
                Failed = outboxItem.Failed,
                FailureReason = outboxItem.FailureReason,
                TraceId = outboxItem.TraceId,
                EmailAddress = outboxItem.EmailAddress // Ensure partition key is set
            };

            await _usersContainer.UpsertItemAsync(cosmosOutboxItem, new PartitionKey(cosmosOutboxItem.EmailAddress));
        }
        catch (CosmosException ex)
        {
            throw new DatabaseFailureException($"Failed to update outbox item with ID {outboxItem.ItemId}", ex);
        }
    }
}