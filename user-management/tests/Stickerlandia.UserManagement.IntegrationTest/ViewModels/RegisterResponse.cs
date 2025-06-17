using System.Text.Json.Serialization;

#pragma warning disable CA1812 // Used for JSON manipulation

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

internal sealed class RegisterResponse
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";
}