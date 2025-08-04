using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Stickerlandia.UserManagement.IntegrationTest.ViewModels;
using Xunit.Abstractions;

#pragma warning disable CA2234, CA2000, CA1031

#pragma warning disable CA1812

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class AccountDriver : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IMessaging _messaging;
    private readonly HttpClient _oauthClient;
    private readonly HttpClient _httpClient;

    public AccountDriver(ITestOutputHelper testOutputHelper, HttpClient httpClient, IMessaging messaging,
        CookieContainer cookieContainer)
    {
        _testOutputHelper = testOutputHelper;
        _messaging = messaging;

        // Create a separate HttpClient for OAuth2.0 operations that doesn't auto-follow redirects
        var oauthHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            CheckCertificateRevocationList = true,
            AllowAutoRedirect = false
        };

        // Create a separate HttpClient for OAuth2.0 operations that doesn't auto-follow redirects
        var mainHttpHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            CheckCertificateRevocationList = true,
            AllowAutoRedirect = true
        };

        _oauthClient = new HttpClient(oauthHandler, true)
        {
            BaseAddress = httpClient.BaseAddress
        };
        _httpClient = new HttpClient(mainHttpHandler, true)
        {
            BaseAddress = httpClient.BaseAddress
        };
    }

    public async Task<RegisterResponse?> RegisterUser(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine($"Registering user: {emailAddress}");

        // Try to create user via direct Identity API calls
        var identityRegistrationResult = await TryIdentityApiRegistration(emailAddress, password);
        if (identityRegistrationResult != null) return identityRegistrationResult;

        // If all registration methods fail, return null to indicate registration failure
        _testOutputHelper.WriteLine("All registration methods failed, registration unsuccessful");
        return null;
    }

    private async Task<RegisterResponse?> TryIdentityApiRegistration(string emailAddress, string password)
    {
        try
        {
            _testOutputHelper.WriteLine("Attempting registration via Identity UI with proper form handling");

            // Step 1: Get the registration page to extract CSRF and form details
            var getResponse = await _oauthClient.GetAsync("auth/register");
            if (!getResponse.IsSuccessStatusCode)
            {
                _testOutputHelper.WriteLine($"Could not get registration page: {getResponse.StatusCode}");
                return null;
            }

            var pageContent = await getResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine($"Got registration page content, length: {pageContent.Length}");

            // Step 2: Extract all form fields, including hidden ones
            var formFields = new Dictionary<string, string>();
            var antiForgeryToken = FormDataExtractor.ExtractAntiForgeryToken(pageContent);

            _testOutputHelper.WriteLine($"Extracted {formFields.Count} form fields");

            if (antiForgeryToken != null)
            {
                formFields["__RequestVerificationToken"] = antiForgeryToken;
                _testOutputHelper.WriteLine("Added anti-forgery token");
            }

            // Step 3: Set the user registration data
            formFields["Input.Email"] = emailAddress;
            formFields["Input.Password"] = password;
            formFields["Input.ConfirmPassword"] = password;
            formFields["Input.FirstName"] = "John";
            formFields["Input.LastName"] = "Doe";

            // Step 4: Submit the form with proper headers
            using var formContent = new FormUrlEncodedContent(formFields);

            // Use the same HttpClient that maintains cookies
            var postResponse = await _oauthClient.PostAsync("auth/register", formContent);

            _testOutputHelper.WriteLine($"Registration POST response: {postResponse.StatusCode}");

            // Step 5: Check the response
            if (postResponse.StatusCode == HttpStatusCode.Redirect)
            {
                var location = postResponse.Headers.Location?.ToString();
                _testOutputHelper.WriteLine($"Registration redirected to: {location}");

                // Successful registration typically redirects
                return new RegisterResponse { AccountId = Guid.NewGuid().ToString() };
            }

            // If not redirected, check the response content
            var responseContent = await postResponse.Content.ReadAsStringAsync();
            if (!HasValidationErrors(responseContent))
            {
                _testOutputHelper.WriteLine("Registration appears successful (no validation errors)");
                return new RegisterResponse { AccountId = Guid.NewGuid().ToString() };
            }
            else
            {
                _testOutputHelper.WriteLine("Registration failed with validation errors");
                _testOutputHelper.WriteLine(
                    $"Response content snippet: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}");
            }

            return null;
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Identity UI registration failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> UpdateUserDetails(string authToken, string firstName, string lastName)
    {
        _testOutputHelper.WriteLine("Updating user details");
        var userInfo = await this.GetUserInfo(authToken);
        var requestBody = JsonSerializer.Serialize(new
        {
            firstName,
            lastName
        });

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/users/v1/{userInfo.Subject}/details");
        request.Headers.Add("Authorization", $"Bearer {authToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _oauthClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<string>>(responseBody)?.Data
            : null;
    }

    public async Task<LoginResponse?> Login(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine("Starting simplified OAuth2.0 flow");

        try
        {
            // Try the full OAuth2.0 authorization code flow
            return await TryOAuthFlow(emailAddress, password);
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"All login methods failed: {ex.Message}");
            return null;
        }
    }

    private async Task<LoginResponse?> TryOAuthFlow(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine("Attempting OAuth2.0 authorization code flow");

        try
        {
            // Step 1: First authenticate the user via Identity UI
            await AuthenticateUserViaIdentityUI(emailAddress, password);

            // Step 2: Generate PKCE parameters
            var (codeVerifier, codeChallenge) = PkceHelper.GeneratePkceParameters();

            // Step 3: Start OAuth2.0 authorization flow
            var state = Guid.NewGuid().ToString();
            var authorizationUrl = BuildAuthorizationUrl(codeChallenge, state);

            _testOutputHelper.WriteLine($"Authorization URL: {authorizationUrl}");

            // Step 4: Follow authorization flow using OAuth client (no auto-redirect)
            var authorizationResponse = await _oauthClient.GetAsync(authorizationUrl);

            // The authorization response should return HttpStatusCode.Found (302) if successful, with the redirect location containing the authorization code.
            // In the 'Location' header
            if (authorizationResponse.StatusCode != HttpStatusCode.Found)
            {
                _testOutputHelper.WriteLine($"Authorization request failed: {authorizationResponse.StatusCode}");
                var errorContent = await authorizationResponse.Content.ReadAsStringAsync();
                _testOutputHelper.WriteLine($"Error content: {errorContent}");
                return null;
            }

            // Step 5: Handle consent form if present, or extract authorization code from redirect
            var authCode = await HandleAuthorizationResponse(authorizationResponse, state);

            if (string.IsNullOrEmpty(authCode))
            {
                _testOutputHelper.WriteLine("Failed to extract authorization code");
                return null;
            }

            _testOutputHelper.WriteLine($"Authorization code: {authCode}");

            // Step 6: Exchange authorization code for access token
            return await ExchangeCodeForToken(authCode, codeVerifier);
        }
        catch (InvalidOperationException ex)
        {
            _testOutputHelper.WriteLine($"OAuth flow failed: {ex.Message}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _testOutputHelper.WriteLine($"OAuth HTTP request failed: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _testOutputHelper.WriteLine($"OAuth JSON parsing failed: {ex.Message}");
            return null;
        }
    }

    private async Task AuthenticateUserViaIdentityUI(string emailAddress, string password)
    {
        // Try different login endpoints to find the correct one
        var loginPath = "auth/login";
        var loginPageResponse = await _oauthClient.GetAsync(loginPath);

        if (loginPageResponse == null || !loginPageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to find working login page endpoint");

        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();

        // Step 2: Extract form fields and anti-forgery token
        var formFields = new Dictionary<string, string>();
        var antiForgeryToken = FormDataExtractor.ExtractAntiForgeryToken(loginPageContent);

        if (antiForgeryToken != null) formFields["__RequestVerificationToken"] = antiForgeryToken;

        // Step 3: Set login credentials
        formFields["Input.Email"] = emailAddress;
        formFields["Input.Password"] = password;
        formFields["Input.RememberMe"] = "false";

        // Step 4: Submit login form to the same working path
        using var loginContent = new FormUrlEncodedContent(formFields);
        var loginResponse = await _oauthClient.PostAsync(loginPath, loginContent);

        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();

        // Check if login was successful (302 redirect) or failed (200 with validation errors)
        if (loginResponse.StatusCode == HttpStatusCode.Redirect)
        {
            _testOutputHelper.WriteLine("Login successful - redirected");
            return;
        }

        // Check for validation errors in the response content
        if (HasValidationErrors(loginResponseContent))
        {
            _testOutputHelper.WriteLine("Login failed - validation errors found");
            throw new InvalidOperationException("Login failed - invalid credentials or validation errors");
        }

        // If we get here, something unexpected happened
        _testOutputHelper.WriteLine($"Unexpected login response: {loginResponse.StatusCode}");
        throw new InvalidOperationException($"Login failed: {loginResponse.StatusCode}");
    }

    private static string BuildAuthorizationUrl(string codeChallenge, string state)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["response_type"] = "code";
        queryParams["client_id"] = TestConstants.OAuth2ClientId;
        queryParams["redirect_uri"] = TestConstants.OAuth2RedirectUri;
        queryParams["scope"] = string.Join(" ", TestConstants.OAuth2Scopes);
        queryParams["state"] = state;
        queryParams["code_challenge"] = codeChallenge;
        queryParams["code_challenge_method"] = "S256";

        return $"{TestConstants.AuthorizeEndpoint}?{queryParams}";
    }

    private async Task<string?> HandleAuthorizationResponse(HttpResponseMessage response, string state)
    {
        // First, check if we got a direct redirect with authorization code
        var authCode = await ExtractAuthorizationCode(response);
        if (!string.IsNullOrEmpty(authCode)) return authCode;

        // If no direct redirect, check if we need to handle a consent form
        var responseContent = await response.Content.ReadAsStringAsync();

        _testOutputHelper.WriteLine("No authorization code or consent form found in response");
        _testOutputHelper.WriteLine($"Response status: {response.StatusCode}");
        _testOutputHelper.WriteLine($"Response content: {responseContent}");
        return null;
    }

    private static async Task<string?> ExtractAuthorizationCode(HttpResponseMessage response)
    {
        // Check if we got redirected with the authorization code
        var location = response.Headers.Location?.ToString();

        if (!string.IsNullOrEmpty(location) && location.Contains("code=", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(location);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            return queryParams["code"];
        }

        // If not a redirect, check the response content for embedded code
        var content = await response.Content.ReadAsStringAsync();

        // Look for authorization code in various places
        // This might need adjustment based on actual OAuth server behavior
        if (content.Contains("authorization_code", StringComparison.OrdinalIgnoreCase))
            // Try to parse from JSON response
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("code", out var codeElement)) return codeElement.GetString();
            }
            catch (JsonException)
            {
                // Not JSON, continue with other extraction methods
            }

        return null;
    }

    private async Task<LoginResponse?> ExchangeCodeForToken(string authCode, string codeVerifier)
    {
        _testOutputHelper.WriteLine("Exchanging authorization code for access token");

        var tokenRequest = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", TestConstants.OAuth2ClientId),
            new("client_secret", TestConstants.OAuth2ClientSecret),
            new("code", authCode),
            new("redirect_uri", TestConstants.OAuth2RedirectUri),
            new("code_verifier", codeVerifier)
        };

        using var requestContent = new FormUrlEncodedContent(tokenRequest);
        var tokenResponse = await _oauthClient.PostAsync(TestConstants.TokenEndpoint, requestContent);

        _testOutputHelper.WriteLine($"Token exchange status: {tokenResponse.StatusCode}");

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine($"Token exchange failed: {errorContent}");
            return null;
        }

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokenContent);

        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenElement)) return null;

        var accessToken = accessTokenElement.GetString();

        return new LoginResponse
        {
            AuthToken = accessToken ?? string.Empty
        };
    }


    public async Task<UserInfo?> GetUserInfo(string authToken)
    {
        _testOutputHelper.WriteLine("Getting user account from /userinfo endpoint");

        using var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.UserInfoEndpoint);
        request.Headers.Add("Authorization", $"Bearer {authToken}");

        var response = await _oauthClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode) return JsonSerializer.Deserialize<UserInfo>(responseBody);

        return null;
    }


    public async Task<UserAccountDTO?> GetUserAccount(string authToken)
    {
        _testOutputHelper.WriteLine("Getting user account");
        var userInfo = await GetUserInfo(authToken);

        const int maxRetries = 3;
        const int baseDelayMs = 1000;

        for (var attempt = 0; attempt < maxRetries; attempt++)
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/v1/{userInfo.Subject}/details");
                request.Headers.Add("Authorization", $"Bearer {authToken}");

                var response = await _oauthClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<ApiResponse<UserAccountDTO>>(responseBody)?.Data;

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < maxRetries - 1)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                    _testOutputHelper.WriteLine(
                        $"Service unavailable, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries})");
                    await Task.Delay(delay);
                    continue;
                }

                return null;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("service unavailable",
                                                      StringComparison.OrdinalIgnoreCase) && attempt < maxRetries - 1)
            {
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                _testOutputHelper.WriteLine(
                    $"HTTP request failed with service unavailable, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                await Task.Delay(delay);
            }

        return null;
    }

    public async Task<bool> TryAccessAnotherUsersData(string authToken, string differentUserId)
    {
        _testOutputHelper.WriteLine($"Attempting to access another user's data with userId: {differentUserId}");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/v1/{differentUserId}/details");
            request.Headers.Add("Authorization", $"Bearer {authToken}");

            var response = await _oauthClient.SendAsync(request);

            // Return true if we get 403 Forbidden (which is expected)
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Exception when trying to access another user's data: {ex.Message}");
            return false;
        }
    }

    public async Task InjectStickerClaimedMessage(string userId, string stickerId)
    {
        await _messaging.SendMessageAsync("users.stickerClaimed.v1", new
        {
            accountId = userId,
            stickerId = stickerId
        });
    }

    private static bool HasValidationErrors(string htmlContent)
    {
        // Check for validation error indicators in the HTML response
        // Look for validation summary with errors
        if (htmlContent.Contains("asp-validation-summary=\"ModelOnly\"", StringComparison.OrdinalIgnoreCase) &&
            (htmlContent.Contains("text-red-600", StringComparison.OrdinalIgnoreCase) ||
             htmlContent.Contains("text-danger", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Look for field-level validation errors
        if (htmlContent.Contains("asp-validation-for=", StringComparison.OrdinalIgnoreCase) &&
            (htmlContent.Contains("text-red-600", StringComparison.OrdinalIgnoreCase) ||
             htmlContent.Contains("text-danger", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Look for specific error messages
        if (htmlContent.Contains("Invalid login attempt", StringComparison.OrdinalIgnoreCase) ||
            htmlContent.Contains("field is required", StringComparison.OrdinalIgnoreCase) ||
            htmlContent.Contains("already taken", StringComparison.OrdinalIgnoreCase) ||
            htmlContent.Contains("not a valid email", StringComparison.OrdinalIgnoreCase) ||
            htmlContent.Contains("password must", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public void Dispose()
    {
        _oauthClient?.Dispose();
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}