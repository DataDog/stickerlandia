// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.

using System.Net;
using System.Text;
using System.Text.Json;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, RegisteredUser> _registeredUsers = new();
    private readonly Dictionary<string, string> _tokenToEmail = new(); // Map tokens to user emails
    
    private sealed record RegisteredUser(string Email, string Password, string FirstName, string LastName, string AccountId);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay
        
        var path = request.RequestUri?.PathAndQuery ?? "";
        var method = request.Method.Method;
        
        // Mock Identity UI registration page GET
        if (method == "GET" && path.Contains("Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(GenerateRegistrationPageHtml(), Encoding.UTF8, "text/html")
            };
        }
        
        // Mock Identity UI registration POST
        if (method == "POST" && path.Contains("Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
        {
            var formData = await ParseFormDataAsync(request);
            var email = formData.GetValueOrDefault("Input.Email", "");
            var password = formData.GetValueOrDefault("Input.Password", "");
            var confirmPassword = formData.GetValueOrDefault("Input.ConfirmPassword", "");
            var firstName = formData.GetValueOrDefault("Input.FirstName", "");
            var lastName = formData.GetValueOrDefault("Input.LastName", "");
            
            // Validate inputs
            if (!IsValidRegistration(email, password, confirmPassword, firstName, lastName))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GenerateRegistrationPageWithErrors(), Encoding.UTF8, "text/html")
                };
            }
            
            // Register the user
            var accountId = Guid.NewGuid().ToString();
            _registeredUsers[email] = new RegisteredUser(email, password, firstName, lastName, accountId);
            
            // Return redirect (successful registration)
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://localhost:51545/Identity/Account/RegisterConfirmation");
            return response;
        }
        
        // Mock Identity UI login page GET
        if (method == "GET" && path.Contains("Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(GenerateLoginPageHtml(), Encoding.UTF8, "text/html")
            };
        }
        
        // Mock Identity UI login POST
        if (method == "POST" && path.Contains("Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            var formData = await ParseFormDataAsync(request);
            var email = formData.GetValueOrDefault("Input.Email", "");
            var password = formData.GetValueOrDefault("Input.Password", "");
            
            if (_registeredUsers.TryGetValue(email, out var user) && user.Password == password)
            {
                // Successful login - redirect
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.Location = new Uri("https://localhost:51545/");
                return response;
            }
            else
            {
                // Failed login - return page with errors
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GenerateLoginPageWithErrors(), Encoding.UTF8, "text/html")
                };
            }
        }
        
        // Mock OAuth2.0 authorize endpoint
        if (method == "GET" && path.Contains("connect/authorize", StringComparison.OrdinalIgnoreCase))
        {
            // Return redirect with auth code
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://localhost:3000/callback?code=mock_auth_code_123&state=" + ExtractStateFromQuery(path));
            return response;
        }
        
        // Mock OAuth2.0 token endpoint
        if (method == "POST" && path.Contains("connect/token", StringComparison.OrdinalIgnoreCase))
        {
            var formData = await ParseFormDataAsync(request);
            var grantType = formData.GetValueOrDefault("grant_type", "");
            
            // For password grant, validate credentials
            if (grantType == "password")
            {
                var username = formData.GetValueOrDefault("username", "");
                var password = formData.GetValueOrDefault("password", "");
                
                if (!_registeredUsers.TryGetValue(username, out var user) || user.Password != password)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new { error = "invalid_grant" }), Encoding.UTF8, "application/json")
                    };
                }
            }
            
            var accessToken = "mock_access_token_" + Guid.NewGuid().ToString("N")[..16];
            
            // For password grant, store the token-to-email mapping
            if (grantType == "password")
            {
                var username = formData.GetValueOrDefault("username", "");
                _tokenToEmail[accessToken] = username;
            }
            else
            {
                // For authorization_code grant, we don't have direct access to user email here
                // In a real implementation, we'd decode the authorization code
                // For testing, we'll assume any valid auth code represents the first registered user
                var firstUser = _registeredUsers.Values.FirstOrDefault();
                if (firstUser != null)
                {
                    _tokenToEmail[accessToken] = firstUser.Email;
                }
            }
            
            var tokenResponse = new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid profile email"
            };
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            };
        }
        
        // Mock API endpoints
        if (method == "GET" && path.Contains("api/users/v1/details", StringComparison.OrdinalIgnoreCase))
        {
            // Extract token from Authorization header
            var authHeader = request.Headers.Authorization?.Parameter;
            if (authHeader == null || !_tokenToEmail.TryGetValue(authHeader, out var email) || !_registeredUsers.TryGetValue(email, out var user))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Unauthorized")
                };
            }
            
            var userDetails = new
            {
                data = new
                {
                    accountId = user.AccountId,
                    emailAddress = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    claimedStickerCount = 0
                }
            };
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userDetails), Encoding.UTF8, "application/json")
            };
        }
        
        if (method == "PUT" && path.Contains("api/users/v1/details", StringComparison.OrdinalIgnoreCase))
        {
            // Extract token from Authorization header
            var authHeader = request.Headers.Authorization?.Parameter;
            if (authHeader == null || !_tokenToEmail.TryGetValue(authHeader, out var email) || !_registeredUsers.TryGetValue(email, out var existingUser))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Unauthorized")
                };
            }
            
            // Parse the update request
            var updateContent = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(updateContent);
            var firstName = doc.RootElement.GetProperty("firstName").GetString() ?? existingUser.FirstName;
            var lastName = doc.RootElement.GetProperty("lastName").GetString() ?? existingUser.LastName;
            
            // Update the stored user data
            _registeredUsers[email] = existingUser with { FirstName = firstName, LastName = lastName };
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\": \"Updated successfully\"}", Encoding.UTF8, "application/json")
            };
        }
        
        // Default 404 for unhandled routes
        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found")
        };
    }
    
    private static string GenerateRegistrationPageHtml()
    {
        return """
        <!DOCTYPE html>
        <html>
        <body>
        <form method="post">
            <input name="__RequestVerificationToken" type="hidden" value="mock_token_123" />
            <input name="Input.Email" type="email" />
            <input name="Input.Password" type="password" />
            <input name="Input.ConfirmPassword" type="password" />
            <input name="Input.FirstName" type="text" />
            <input name="Input.LastName" type="text" />
            <button type="submit">Register</button>
        </form>
        </body>
        </html>
        """;
    }
    
    private static string GenerateRegistrationPageWithErrors()
    {
        return """
        <!DOCTYPE html>
        <html>
        <body>
        <div asp-validation-summary="ModelOnly" class="text-red-600 text-sm" role="alert">
            Registration failed due to validation errors.
        </div>
        <form method="post">
            <input name="__RequestVerificationToken" type="hidden" value="mock_token_123" />
            <input name="Input.Email" type="email" />
            <input name="Input.Password" type="password" />
            <input name="Input.ConfirmPassword" type="password" />
            <input name="Input.FirstName" type="text" />
            <input name="Input.LastName" type="text" />
            <button type="submit">Register</button>
        </form>
        </body>
        </html>
        """;
    }
    
    private static string GenerateLoginPageHtml()
    {
        return """
        <!DOCTYPE html>
        <html>
        <body>
        <form method="post">
            <input name="__RequestVerificationToken" type="hidden" value="mock_token_123" />
            <input name="Input.Email" type="email" />
            <input name="Input.Password" type="password" />
            <input name="Input.RememberMe" type="checkbox" value="false" />
            <button type="submit">Log in</button>
        </form>
        </body>
        </html>
        """;
    }
    
    private static string GenerateLoginPageWithErrors()
    {
        return """
        <!DOCTYPE html>
        <html>
        <body>
        <div asp-validation-summary="ModelOnly" class="text-danger" role="alert">
            Invalid login attempt.
        </div>
        <form method="post">
            <input name="__RequestVerificationToken" type="hidden" value="mock_token_123" />
            <input name="Input.Email" type="email" />
            <input name="Input.Password" type="password" />
            <input name="Input.RememberMe" type="checkbox" value="false" />
            <button type="submit">Log in</button>
        </form>
        </body>
        </html>
        """;
    }
    
    private static async Task<Dictionary<string, string>> ParseFormDataAsync(HttpRequestMessage request)
    {
        var result = new Dictionary<string, string>();
        
        if (request.Content == null)
            return result;
            
        var content = await request.Content.ReadAsStringAsync();
        var pairs = content.Split('&');
        
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }
        
        return result;
    }
    
    private static string ExtractStateFromQuery(string path)
    {
        var stateIndex = path.IndexOf("state=", StringComparison.OrdinalIgnoreCase);
        if (stateIndex == -1) return "mock_state";
        
        var start = stateIndex + 6;
        var end = path.IndexOf('&', start);
        if (end == -1) end = path.Length;
        
        return path[start..end];
    }
    
    private static bool IsValidRegistration(string email, string password, string confirmPassword, string firstName, string lastName)
    {
        // Email validation
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        // Basic email format validation
        if (!email.Contains('@', StringComparison.Ordinal) || !email.Contains('.', StringComparison.Ordinal))
            return false;
            
        // Check for invalid email patterns
        if (email.StartsWith('@') || 
            email.EndsWith('@') ||
            email.Contains("@@", StringComparison.Ordinal) ||
            !email.Split('@')[1].Contains('.', StringComparison.Ordinal))
            return false;
            
        // Check email length (typical max is around 254 characters)
        if (email.Length > 254)
            return false;
        
        // Password validation
        if (string.IsNullOrWhiteSpace(password))
            return false;
            
        // Password must be at least 6 characters
        if (password.Length < 6)
            return false;
            
        // Password must contain at least one uppercase letter
        if (!password.Any(char.IsUpper))
            return false;
            
        // Password must contain at least one lowercase letter
        if (!password.Any(char.IsLower))
            return false;
            
        // Password must contain at least one digit
        if (!password.Any(char.IsDigit))
            return false;
            
        // Password must contain at least one special character
        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c, StringComparison.Ordinal)))
            return false;
            
        // Passwords must match
        if (password != confirmPassword)
            return false;
        
        // Basic name validation
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return false;
        
        return true;
    }
}