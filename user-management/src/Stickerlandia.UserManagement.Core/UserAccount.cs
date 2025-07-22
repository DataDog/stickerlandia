// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Stickerlandia.UserManagement.Core.RegisterUser;
using Stickerlandia.UserManagement.Core.UpdateUserDetails;

namespace Stickerlandia.UserManagement.Core;

public record AccountId
{
    public string Value { get; init; }

    public AccountId(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        Value = value;
    }
}

public enum AccountType
{
    User,
    Admin
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
        _domainEvents = new List<DomainEvent>();
    }

    public static UserAccount Register(string emailAddress, string password, string firstName,
        string lastName, AccountType accountType)
    {
        if (!IsValidEmail(emailAddress)) throw new InvalidUserException("Invalid email address");

        var userAccount = new UserAccount
        {
            Id = new AccountId(emailAddress),
            Password = password,
            EmailAddress = emailAddress,
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
        AccountId id,
        string emailAddress,
        string firstName,
        string lastName,
        int claimedStickerCount,
        DateTime dateCreated,
        AccountTier accountTier,
        AccountType accountType)
    {
        return new UserAccount
        {
            Id = id,
            EmailAddress = emailAddress,
            DateCreated = dateCreated,
            AccountTier = accountTier,
            AccountType = accountType,
            FirstName = firstName,
            LastName = lastName,
            ClaimedStickerCount = claimedStickerCount
        };
    }

    public AccountId? Id { get; private set; }

    public string EmailAddress { get; private set; } = string.Empty;

    public int AccountAge => (DateTime.UtcNow - DateCreated).Days;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    public DateTime DateCreated { get; private set; }

    public AccountTier AccountTier { get; private set; }

    public AccountType AccountType { get; private set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    public int ClaimedStickerCount { get; private set; }

    internal bool Changed { get; private set; }

    internal static bool IsValidEmail(string email)
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