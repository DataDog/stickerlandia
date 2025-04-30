using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

public record LoginResponse
{
    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; } = "";
}

public record ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}