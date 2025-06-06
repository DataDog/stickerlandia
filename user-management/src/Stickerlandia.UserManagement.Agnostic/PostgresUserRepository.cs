using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

public class PostgresUserRepository(
    UserManagementDbContext dbContext,
    ILogger<PostgresUserRepository> logger,
    UserManager<PostgresUserAccount> userManager)
    : IUsers, IOutbox
{
    public async Task<UserAccount> Add(UserAccount userAccount)
    {
        try
        {
            // Check if user already exists
            if (await userManager.FindByEmailAsync(userAccount.EmailAddress) is not null)
                throw new UserExistsException();

            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create user entity
                var userEntity = new PostgresUserAccount
                {
                    Id = userAccount.Id.Value,
                    UserName = userAccount.EmailAddress,
                    Email = userAccount.EmailAddress,
                    FirstName = userAccount.FirstName,
                    LastName = userAccount.LastName,
                    ClaimedStickerCount = userAccount.ClaimedStickerCount,
                    DateCreated = userAccount.DateCreated,
                    AccountTier = userAccount.AccountTier,
                    AccountType = userAccount.AccountType
                };

                dbContext.Users.Add(userEntity);

                // Create outbox items for domain events
                foreach (var evt in userAccount.DomainEvents)
                {
                    var outboxItem = new PostgresOutboxItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventType = evt.EventName,
                        EventData = evt.ToJsonString(),
                        EmailAddress = userAccount.EmailAddress,
                        EventTime = DateTime.UtcNow,
                        Processed = false,
                        Failed = false
                    };

                    await dbContext.OutboxItems.AddAsync(outboxItem);
                }

                var result = await userManager.CreateAsync(userEntity, userAccount.Password);

                if (!result.Succeeded)
                {
                    throw new DatabaseFailureException("Failure creating user in database");
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return userAccount;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create account due to database error");
            throw new DatabaseFailureException("Failed to create account", ex);
        }
        catch (UserExistsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create account");
            throw new DatabaseFailureException("Failed to create account", ex);
        }
    }

    public async Task UpdateAccount(UserAccount userAccount)
    {
        try
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Find existing user
                var existingUser = await dbContext.Users.FindAsync(userAccount.Id.Value);
                if (existingUser == null) throw new DatabaseFailureException("User account not found");

                // Update properties
                existingUser.Email = userAccount.EmailAddress;
                existingUser.FirstName = userAccount.FirstName;
                existingUser.LastName = userAccount.LastName;
                existingUser.ClaimedStickerCount = userAccount.ClaimedStickerCount;
                existingUser.DateCreated = userAccount.DateCreated;
                existingUser.AccountTier = userAccount.AccountTier;
                existingUser.AccountType = userAccount.AccountType;

                dbContext.Users.Update(existingUser);

                // Create outbox items for domain events
                foreach (var evt in userAccount.DomainEvents)
                {
                    var outboxItem = new PostgresOutboxItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        EventType = evt.EventName,
                        EventData = evt.ToJsonString(),
                        EmailAddress = userAccount.EmailAddress,
                        EventTime = DateTime.UtcNow,
                        Processed = false,
                        Failed = false
                    };

                    await dbContext.OutboxItems.AddAsync(outboxItem);
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to update account due to database error");
            throw new DatabaseFailureException("Failed to update account", ex);
        }
        catch (DatabaseFailureException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update account");
            throw new DatabaseFailureException("Failed to update account", ex);
        }
    }

    public async Task<UserAccount?> WithIdAsync(AccountId accountId)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == accountId.Value);

            if (user == null) return null;

            return UserAccount.From(
                new AccountId(user.Id),
                user.Email,
                user.FirstName,
                user.LastName,
                user.ClaimedStickerCount,
                user.DateCreated,
                user.AccountTier,
                user.AccountType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by ID {Id}", accountId.Value);
            return null;
        }
    }

    public async Task<UserAccount?> WithEmailAsync(string emailAddress)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == emailAddress);

            if (user == null) return null;

            return UserAccount.From(
                new AccountId(user.Id),
                user.Email,
                user.FirstName,
                user.LastName,
                user.ClaimedStickerCount,
                user.DateCreated,
                user.AccountTier,
                user.AccountType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email {Email}", emailAddress);
            return null;
        }
    }

    public async Task<bool> DoesEmailExistAsync(string emailAddress)
    {
        try
        {
            return await dbContext.Users.AnyAsync(u => u.Email == emailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if email exists {Email}", emailAddress);
            return false;
        }
    }

    public async Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        try
        {
            var items = await dbContext.OutboxItems
                .Where(o => o.Processed == false && o.Failed == false)
                .Take(maxCount)
                .ToListAsync();

            return items.Select(item => new OutboxItem
            {
                ItemId = item.Id,
                EmailAddress = item.EmailAddress,
                EventType = item.EventType,
                EventData = item.EventData,
                EventTime = item.EventTime,
                Processed = item.Processed,
                Failed = item.Failed,
                FailureReason = item.FailureReason,
                TraceId = item.TraceId
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unprocessed outbox items");
            return new List<OutboxItem>();
        }
    }

    public async Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        try
        {
            var item = await dbContext.OutboxItems.FindAsync(outboxItem.ItemId);

            if (item == null) throw new DatabaseFailureException($"Outbox item with ID {outboxItem.ItemId} not found");

            item.Processed = outboxItem.Processed;
            item.Failed = outboxItem.Failed;
            item.FailureReason = outboxItem.FailureReason;
            item.TraceId = outboxItem.TraceId;

            dbContext.OutboxItems.Update(item);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseFailureException($"Failed to update outbox item with ID {outboxItem.ItemId}", ex);
        }
    }
}