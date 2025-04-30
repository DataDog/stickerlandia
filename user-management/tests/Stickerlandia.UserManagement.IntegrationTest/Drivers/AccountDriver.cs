using System.Text;
using System.Text.Json;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Stickerlandia.UserManagement.IntegrationTest.Hooks;
using Stickerlandia.UserManagement.IntegrationTest.ViewModels;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public class AccountDriver
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly TestSetupFixture _testSetupFixture;
    
    public AccountDriver(ITestOutputHelper testOutputHelper, TestSetupFixture testSetupFixture)
    {
        _testOutputHelper = testOutputHelper;
        _testSetupFixture = testSetupFixture;
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

        var registerResult = await _testSetupFixture.HttpClient.PostAsync("api/users/v1/register",
            new StringContent(requestBody, Encoding.Default, "application/json"));

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

        var request = new HttpRequestMessage(HttpMethod.Put, "api/users/v1/details");
        request.Headers.Add("Authorization", $"Bearer {authToken}");
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _testSetupFixture.HttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<string>>(responseBody)?.Data
            : null;
    }

    public async Task<LoginResponse?> Login(string emailAddress, string password)
    {
        _testOutputHelper.WriteLine("Starting login request");
        
        var requestBody = JsonSerializer.Serialize(new
        {
            emailAddress,
            password
        });

        var loginResult = await _testSetupFixture.HttpClient.PostAsync("api/users/v1/login",
            new StringContent(requestBody, Encoding.Default, "application/json"));

        _testOutputHelper.WriteLine($"Login status code is {loginResult.StatusCode}");

        var loginResultBody = await loginResult.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(loginResultBody)) return null;

        var parsedBody = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(loginResultBody);

        return loginResult.IsSuccessStatusCode
            ? parsedBody?.Data
            : null;
    }

    public async Task<UserAccountDTO?> GetUserAccount(string authToken)
    {
        _testOutputHelper.WriteLine("Getting user account");
        
        var request = new HttpRequestMessage(HttpMethod.Get, "api/users/v1/details");
        request.Headers.Add("Authorization", $"Bearer {authToken}");

        var response = await _testSetupFixture.HttpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<UserAccountDTO>>(responseBody)?.Data
            : null;
    }

    public async Task InjectStickerClaimedMessage(string userId, string stickerId)
    {
        await _testSetupFixture.Messaging.SendMessageAsync("users.stickerClaimed.v1", new
        {
            accountId = userId,
            stickerId = stickerId
        });
    }
}