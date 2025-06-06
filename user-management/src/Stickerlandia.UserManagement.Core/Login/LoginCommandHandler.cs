// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Diagnostics;

namespace Stickerlandia.UserManagement.Core.Login;

public class LoginCommandHandler(IUsers users, IAuthService authService)
{
    public async Task<LoginResponse> Handle(LoginCommand? request)
    {
        try
        {
            if (request == null || !request.IsValid()) throw new ArgumentException("Invalid LoginCommand");

            var account = await users.WithEmailAsync(request.EmailAddress);

            if (account == null) throw new LoginFailedException("User not found.");

            var identity =
                await authService.VerifyPassword(request.EmailAddress, request.Password, request.Scopes);

            return new LoginResponse
            {
                Identity = identity
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