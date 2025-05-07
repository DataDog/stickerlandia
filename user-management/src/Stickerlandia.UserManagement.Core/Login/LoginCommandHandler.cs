// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;
using Stickerlandia.UserManagement.Core.Auth;

namespace Stickerlandia.UserManagement.Core.Login;

public class LoginCommandHandler(IUsers users, IAuthService authService)
{
    public async Task<LoginResponse> Handle(LoginCommand request)
    {
        try
        {
            if (request == null || !request.IsValid())
            {
                throw new ArgumentException("Invalid LoginCommand");
            }
            
            var account = await users.WithEmailAsync(request.EmailAddress);

            if (account == null) throw new LoginFailedException("Invalid password");

            var isValidPassword = account.VerifyPassword(request.Password);

            if (!isValidPassword) throw new LoginFailedException("Invalid password");

            var token = authService.GenerateAuthToken(account);

            return new LoginResponse
            {
                AuthToken = token
            };
        }
        catch (LoginFailedException ex)
        {
            Activity.Current?.AddTag("login.failed", true);
            Activity.Current?.AddTag("error.message", ex.Message);
            
            throw;
        }
    }
}