// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

namespace Stickerlandia.UserManagement.Core;

public interface IUsers
{
    /// <summary>
    /// Persist a new user
    /// </summary>
    /// <param name="userAccount">The <see cref="UserAccount"/> to add.</param>
    /// <returns>A <see cref="UserAccount"/>.</returns>
    Task<UserAccount> Add(UserAccount userAccount);
    
    /// <summary>
    /// Update an existing user account in the database
    /// </summary>
    /// <param name="userAccount">The <see cref="UserAccount"/> to add.</param>
    /// <returns>N/A</returns>
    Task UpdateAccount(UserAccount userAccount);
    
    /// <summary>
    /// Retrieve a user by their unique identifier
    /// </summary>
    /// <param name="accountId">The account id to search for</param>
    /// <returns>A <see cref="UserAccount"/>, or null if the user isn't found.</returns>
    Task<UserAccount?> WithIdAsync(AccountId accountId);
    
    /// <summary>
    /// Retrieve a user by their email address
    /// </summary>
    /// <param name="emailAddress">The email address to search for</param>
    /// <returns>A <see cref="UserAccount"/>, or null if the user isn't found.</returns>
    Task<UserAccount?> WithEmailAsync(string emailAddress);
    
    /// <summary>
    /// Check to see if an email address has already been registered.
    /// </summary>
    /// <param name="emailAddress">The email address to check for.</param>
    /// <returns>true/false if the email exists or does not respectively</returns>
    Task<bool> DoesEmailExistAsync(string emailAddress);
}
