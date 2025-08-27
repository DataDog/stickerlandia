// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Stickerlandia.UserManagement.Core.Outbox;

namespace Stickerlandia.UserManagement.Core.RegisterUser;

public class RegisterCommandHandler
{
    private readonly UserManager<PostgresUserAccount> _userManager;
    private readonly IOutbox _outbox;

    public RegisterCommandHandler(
        UserManager<PostgresUserAccount> userManager,
        IOutbox outbox)
    {
        _userManager = userManager;
        _outbox = outbox;
    }

    public async Task<RegisterResponse> Handle(RegisterUserCommand command, AccountType accountType)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(RegisterUserCommand));

        try
        {
            var existingEmail = await _userManager.FindByEmailAsync(command.EmailAddress);

            if (existingEmail != null)
            {
                throw new UserExistsException("A user with this email address already exists.");
            }

            var user = PostgresUserAccount.New(command.EmailAddress, command.FirstName, command.LastName);
            var createUserResult = await _userManager.CreateAsync(user, command.Password);
            

            if (createUserResult.Succeeded)
            {
                switch (accountType)
                {
                    case AccountType.User:
                        await _userManager.AddToRoleAsync(user, UserTypes.User);
                        break;
                    case AccountType.Admin:
                        await _userManager.AddToRoleAsync(user, UserTypes.Admin);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(accountType), accountType, null);
                }
                
                var userId = await _userManager.GetUserIdAsync(user);

                await _outbox.StoreEventFor(userId, new UserRegisteredEvent
                {
                    AccountId = userId
                });

                return new RegisterResponse
                {
                    AccountId = userId,
                    Account = user
                };
            }

            var response = new RegisterResponse();

            foreach (var error in createUserResult.Errors) response.Errors.Add(error.Description);
            
            //transactionScope.Complete();

            return response;
        }
        catch (UserExistsException ex)
        {
            Activity.Current?.AddTag("user.exists", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            Activity.Current?.AddTag("user.registration.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);

            throw;
        }
    }
}