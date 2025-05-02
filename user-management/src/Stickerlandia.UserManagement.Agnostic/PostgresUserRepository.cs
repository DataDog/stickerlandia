using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stickerlandia.UserManagement.Core;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Agnostic;

// Database entity model for user accounts
public class PostgresUserAccount
{
    public string Id { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ClaimedStickerCount { get; set; }
    public DateTime DateCreated { get; set; }
    public AccountTier AccountTier { get; set; }
    public AccountType AccountType { get; set; }
}

// Database entity model for outbox items
public class PostgresOutboxItem
{
    public string Id { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public string? TraceId { get; set; }
}

// DbContext for the user management database
public class UserManagementDbContext : DbContext
{
    public DbSet<PostgresUserAccount> Users { get; set; } = null!;
    public DbSet<PostgresOutboxItem> OutboxItems { get; set; } = null!;

    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PostgresUserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmailAddress).IsUnique();
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmailAddress).HasColumnName("email_address");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.ClaimedStickerCount).HasColumnName("claimed_sticker_count");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.AccountTier).HasColumnName("account_tier");
            entity.Property(e => e.AccountType).HasColumnName("account_type");
        });

        modelBuilder.Entity<PostgresOutboxItem>(entity =>
        {
            entity.ToTable("outbox_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmailAddress).HasColumnName("email_address");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.EventData).HasColumnName("event_data");
            entity.Property(e => e.EventTime).HasColumnName("event_time");
            entity.Property(e => e.Processed).HasColumnName("processed");
            entity.Property(e => e.Failed).HasColumnName("failed");
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
        });
    }
}

public class PostgresUserRepository : IUsers, IOutbox
{
    private readonly UserManagementDbContext _dbContext;
    private readonly ILogger<PostgresUserRepository> _logger;
    private readonly IConfiguration _configuration;

    public PostgresUserRepository(UserManagementDbContext dbContext, ILogger<PostgresUserRepository> logger, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserAccount> Add(UserAccount userAccount)
    {
        try
        {
            // Check if user already exists
            if (await _dbContext.Users.AnyAsync(u => u.EmailAddress == userAccount.EmailAddress))
            {
                throw new UserExistsException();
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create user entity
                var userEntity = new PostgresUserAccount
                {
                    Id = userAccount.Id.Value,
                    EmailAddress = userAccount.EmailAddress,
                    PasswordHash = userAccount.Password,
                    FirstName = userAccount.FirstName,
                    LastName = userAccount.LastName,
                    ClaimedStickerCount = userAccount.ClaimedStickerCount,
                    DateCreated = userAccount.DateCreated,
                    AccountTier = userAccount.AccountTier,
                    AccountType = userAccount.AccountType
                };

                await _dbContext.Users.AddAsync(userEntity);

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

                    await _dbContext.OutboxItems.AddAsync(outboxItem);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return userAccount;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create account due to database error");
            throw new DatabaseFailureException("Failed to create account", ex);
        }
        catch (UserExistsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account");
            throw new DatabaseFailureException("Failed to create account", ex);
        }
    }

    public async Task UpdateAccount(UserAccount userAccount)
    {
        try
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Find existing user
                var existingUser = await _dbContext.Users.FindAsync(userAccount.Id.Value);
                if (existingUser == null)
                {
                    throw new DatabaseFailureException("User account not found");
                }

                // Update properties
                existingUser.EmailAddress = userAccount.EmailAddress;
                existingUser.PasswordHash = userAccount.Password;
                existingUser.FirstName = userAccount.FirstName;
                existingUser.LastName = userAccount.LastName;
                existingUser.ClaimedStickerCount = userAccount.ClaimedStickerCount;
                existingUser.DateCreated = userAccount.DateCreated;
                existingUser.AccountTier = userAccount.AccountTier;
                existingUser.AccountType = userAccount.AccountType;

                _dbContext.Users.Update(existingUser);

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

                    await _dbContext.OutboxItems.AddAsync(outboxItem);
                }

                await _dbContext.SaveChangesAsync();
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
            _logger.LogError(ex, "Failed to update account due to database error");
            throw new DatabaseFailureException("Failed to update account", ex);
        }
        catch (DatabaseFailureException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update account");
            throw new DatabaseFailureException("Failed to update account", ex);
        }
    }

    public async Task<UserAccount?> WithIdAsync(AccountId accountId)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == accountId.Value);

            if (user == null)
            {
                return null;
            }

            return UserAccount.From(
                new AccountId(user.Id),
                user.EmailAddress,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.ClaimedStickerCount,
                user.DateCreated,
                user.AccountTier,
                user.AccountType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID {Id}", accountId.Value);
            return null;
        }
    }

    public async Task<UserAccount?> WithEmailAsync(string emailAddress)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.EmailAddress == emailAddress);

            if (user == null)
            {
                return null;
            }

            return UserAccount.From(
                new AccountId(user.Id),
                user.EmailAddress,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.ClaimedStickerCount,
                user.DateCreated,
                user.AccountTier,
                user.AccountType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", emailAddress);
            return null;
        }
    }

    public async Task<bool> DoesEmailExistAsync(string emailAddress)
    {
        try
        {
            return await _dbContext.Users.AnyAsync(u => u.EmailAddress == emailAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists {Email}", emailAddress);
            return false;
        }
    }

    public async Task MigrateAsync()
    {
        await _dbContext.Database.MigrateAsync();
    }

    public async Task<List<OutboxItem>> GetUnprocessedItemsAsync(int maxCount = 100)
    {
        try
        {
            var items = await _dbContext.OutboxItems
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
            _logger.LogError(ex, "Error retrieving unprocessed outbox items");
            return new List<OutboxItem>();
        }
    }

    public async Task UpdateOutboxItem(OutboxItem outboxItem)
    {
        try
        {
            var item = await _dbContext.OutboxItems.FindAsync(outboxItem.ItemId);
            
            if (item == null)
            {
                throw new DatabaseFailureException($"Outbox item with ID {outboxItem.ItemId} not found");
            }

            item.Processed = outboxItem.Processed;
            item.Failed = outboxItem.Failed;
            item.FailureReason = outboxItem.FailureReason;
            item.TraceId = outboxItem.TraceId;

            _dbContext.OutboxItems.Update(item);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseFailureException($"Failed to update outbox item with ID {outboxItem.ItemId}", ex);
        }
    }
}
