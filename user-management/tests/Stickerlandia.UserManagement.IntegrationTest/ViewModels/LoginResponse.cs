using System.Text.Json.Serialization;

#pragma warning disable CA1812 // Used for JSON manipulation

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

internal sealed record LoginResponse
{
    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; } = "";
}