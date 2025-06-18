// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.RegisterUser;

public class RegisterCommandHandler(IUsers users)
{
    public async Task<RegisterResponse> Handle(RegisterUserCommand command, AccountType accountType)
    {
        try
        {
            if (command == null || !command.IsValid()) throw new ArgumentException("Invalid LoginCommand");

            // Check if email exists before creating account
            var emailExists = await users.DoesEmailExistAsync(command.EmailAddress);
            if (emailExists) throw new UserExistsException();

            // Use async version for better performance
            var userAccount = UserAccount.Register(command.EmailAddress, command.Password, command.FirstName,
                command.LastName, accountType);

            await users.Add(userAccount);

            return new RegisterResponse
            {
                AccountId = userAccount.Id?.Value
            };
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