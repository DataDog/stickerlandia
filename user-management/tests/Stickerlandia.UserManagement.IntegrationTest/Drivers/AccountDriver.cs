using System.Text;
using System.Text.Json;
using Stickerlandia.UserManagement.IntegrationTest.ViewModels;
using Xunit.Abstractions;

#pragma warning disable CA2234

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal sealed class AccountDriver
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;
    private readonly IMessaging _messaging;
    
    public AccountDriver(ITestOutputHelper testOutputHelper, HttpClient httpClient, IMessaging messaging)
    {
        _testOutputHelper = testOutputHelper;
        _httpClient = httpClient;
        _messaging = messaging;
    }

    public async Task<RegisterResponse?> RegisterUser(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine($"Registering user: {emailAddress}");
        
        var requestBody = JsonSerializer.Serialize(new
        {
            emailAddress,
            password,
            firstName = "John",
            lastName = "Doe"
        });

        using var postBody = new StringContent(requestBody, Encoding.Default, "application/json");

        var registerResult = await _httpClient.PostAsync("api/users/v1/register",
            postBody);

        var body = await registerResult.Content.ReadAsStringAsync();

        return registerResult.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<RegisterResponse>>(body)?.Data
            : null;
    }

    public async Task<string?> UpdateUserDetails(string authToken, string firstName, string lastName)
    {
        _testOutputHelper.WriteLine("Updating user details");
        var requestBody = JsonSerializer.Serialize(new
        {
            firstName,
            lastName,
        });

        using var request = new HttpRequestMessage(HttpMethod.Put, "api/users/v1/details");
        request.Headers.Add("Authorization", $"Bearer {authToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<string>>(responseBody)?.Data
            : null;
    }

    public async Task<LoginResponse?> Login(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine("Starting OAuth2.0 login request");

        var requestBody = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "password"),
            new("username", emailAddress),
            new("password", password),
            new("client_id", "user-authentication"),
            new("client_secret", "388D45FA-B36B-4988-BA59-B187D329C207")
        };

        using var requestContent = new FormUrlEncodedContent(requestBody);
        var tokenResult = await _httpClient.PostAsync("api/users/v1/login", requestContent); // Use the original endpoint

        _testOutputHelper.WriteLine($"OAuth2.0 token status code is {tokenResult.StatusCode}");

        var tokenResultBody = await tokenResult.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(tokenResultBody)) return null;

        using var doc = JsonDocument.Parse(tokenResultBody);
        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            return null;

        var accessToken = accessTokenElement.GetString();
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expiresInElement) ? expiresInElement.GetInt32() : 0;

        return new LoginResponse
        {
            AuthToken = accessToken ?? string.Empty
        };
    }

    public async Task<UserAccountDTO?> GetUserAccount(string authToken)
    {
        _testOutputHelper.WriteLine("Getting user account");
        
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/v1/details");
        request.Headers.Add("Authorization", $"Bearer {authToken}");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<UserAccountDTO>>(responseBody)?.Data
            : null;
    }

    public async Task InjectStickerClaimedMessage(string userId, string stickerId)
    {
        await _messaging.SendMessageAsync("users.stickerClaimed.v1", new
        {
            accountId = userId,
            stickerId = stickerId
        });
    }
}
