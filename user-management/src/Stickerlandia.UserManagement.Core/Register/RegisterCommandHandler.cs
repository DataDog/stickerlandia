// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.Register;

public class RegisterCommandHandler(IUserAccountRepository userAccountRepository)
{
    public async Task<RegisterResponse> Handle(RegisterUserCommand command, AccountType accountType, CancellationToken token = default)
    {
        try
        {
            if (command == null || !command.IsValid())
            {
                throw new ArgumentException("Invalid LoginCommand");
            }
            
            // Check if email exists before creating account
            var emailExists = await userAccountRepository.DoesEmailExistAsync(command.EmailAddress);
            if (emailExists) throw new UserExistsException();

            UserAccount? userAccount = null;

            if (accountType == AccountType.Staff && !command.EmailAddress.EndsWith("@plantbasedpizza.com"))
                throw new InvalidUserException("Not a valid staff email");

            // Use async version for better performance
            userAccount = await UserAccount.CreateAsync(command.EmailAddress, command.Password, command.FirstName, command.LastName, accountType);

            await userAccountRepository.CreateAccount(userAccount);

            return new RegisterResponse
            {
                AccountId = userAccount.Id
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