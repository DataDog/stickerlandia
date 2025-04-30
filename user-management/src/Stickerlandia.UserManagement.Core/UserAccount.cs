// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Stickerlandia.UserManagement.Core;

public enum AccountType
{
    User,
    Staff,
    Admin,
    Driver
}

public enum AccountTier
{
    Std,
    Premium
}

public class UserAccount
{
    private readonly List<DomainEvent> _domainEvents;

    public UserAccount()
    {
        this._domainEvents = new List<DomainEvent>();
    }

    // Async version for better performance in web contexts
    public static async Task<UserAccount> CreateAsync(string emailAddress, string password, string firstName,
        string lastName, AccountType accountType)
    {
        // Validate inputs in parallel for better performance
        var emailTask = Task.Run(() => IsValidEmail(emailAddress));
        var passwordTask = Task.Run(() => IsValidPassword(password));

        await Task.WhenAll(emailTask, passwordTask);

        if (!emailTask.Result) throw new InvalidUserException("Invalid email address");

        if (!passwordTask.Result) throw new InvalidUserException("Invalid password");

        var userAccount = new UserAccount
        {
            Id = Guid.NewGuid().ToString(),
            EmailAddress = emailAddress,
            Password = HashPassword(password),
            AccountType = accountType,
            DateCreated = DateTime.UtcNow,
            AccountTier = AccountTier.Std,
            FirstName = firstName,
            LastName = lastName
        };
        
        userAccount._domainEvents.Add(new UserRegisteredEvent(userAccount));

        return userAccount;
    }

    public static UserAccount From(
        string id,
        string emailAddress,
        string passwordHash,
        string firstName,
        string lastName,
        DateTime dateCreated,
        AccountTier accountTier,
        AccountType accountType)
    {
        return new UserAccount
        {
            Id = id,
            EmailAddress = emailAddress,
            Password = passwordHash,
            DateCreated = dateCreated,
            AccountTier = accountTier,
            AccountType = accountType,
            FirstName = firstName,
            LastName = lastName
        };
    }

    public string Id { get; private set; } = string.Empty;

    public string EmailAddress { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    public int AccountAge => (DateTime.UtcNow - DateCreated).Days;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateTime DateCreated { get; private set; }

    public AccountTier AccountTier { get; private set; }

    public AccountType AccountType { get; private set; }
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;
    
    public int ClaimedStickerCount { get; private set; }

    public string AsAuthenticatedRole()
    {
        switch (AccountType)
        {
            case AccountType.Admin:
                return "admin";
            case AccountType.Staff:
                return "staff";
            case AccountType.Driver:
                return "driver";
            case AccountType.User:
                return "user";
        }

        return "user";
    }

    // More performant password hashing using PBKDF2
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100000, // Iterations - adjust based on performance requirements
            HashAlgorithmName.SHA256,
            32);

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password)
    {
        var hashBytes = Convert.FromBase64String(Password);

        var salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100000,
            HashAlgorithmName.SHA256,
            32);

        for (var i = 0; i < 32; i++)
            if (hashBytes[i + 16] != hash[i])
                return false;

        return true;
    }

    public void StickerOrdered(string stickerId)
    {
        ClaimedStickerCount++;
    }
    
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        if (email.Length > 254)
            return false;

        try
        {
            // Simple regex for better performance while maintaining security
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool IsValidPassword(string password)
    {
        if (password.Length <= 8 || password.Length >= 50)
            return false;

        var hasNumber = false;
        var hasUpperChar = false;
        var hasLowerChar = false;
        var hasSymbol = false;

        // Single pass through the string instead of multiple regex evaluations
        foreach (var c in password)
        {
            if (char.IsDigit(c)) hasNumber = true;
            else if (char.IsUpper(c)) hasUpperChar = true;
            else if (char.IsLower(c)) hasLowerChar = true;
            else if (!char.IsLetterOrDigit(c)) hasSymbol = true;

            // Early return if all criteria are met
            if (hasNumber && hasUpperChar && hasLowerChar && hasSymbol)
                return true;
        }

        return hasNumber && hasUpperChar && hasLowerChar && hasSymbol;
    }
}