using System.Text;
using System.Text.Json;
using Stickerlandia.UserManagement.IntegrationTest.ViewModels;
using Xunit.Abstractions;

namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public class AccountDriver
{
    private static string BaseUrl = $"{TestConstants.DefaultTestUrl}/api";
    private readonly IMessaging _messaging;
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _testOutputHelper;

    public AccountDriver(ITestOutputHelper testOutputHelper, IMessaging? messaging = null)
    {
        _testOutputHelper = testOutputHelper;
        _httpClient = new HttpClient();
        _messaging = messaging ?? new AzureServiceBusMessaging(TestConstants.DefaultMessagingConnection);
    }

    public async Task<RegisterResponse?> RegisterUser(string emailAddress, string password)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            emailAddress,
            password,
            firstName = "John",
            lastName = "Doe"
        });

        var registerResult = await _httpClient.PostAsync(new Uri($"{BaseUrl}/register"),
            new StringContent(requestBody, Encoding.Default, "application/json"));

        var body = await registerResult.Content.ReadAsStringAsync();

        return registerResult.IsSuccessStatusCode
            ? JsonSerializer.Deserialize<ApiResponse<RegisterResponse>>(body)?.Data
            : null;
    }

    public async Task<LoginResponse?> Login(string emailAddress, string password)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            emailAddress,
            password
        });

        _testOutputHelper.WriteLine("Starting login request");

        var loginResult = await _httpClient.PostAsync(new Uri($"{BaseUrl}/login"),
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
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{BaseUrl}/details"));
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