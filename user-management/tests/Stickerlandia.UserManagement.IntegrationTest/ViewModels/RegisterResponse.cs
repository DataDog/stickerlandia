using System.Text.Json.Serialization;

namespace Stickerlandia.UserManagement.IntegrationTest.ViewModels;

public class RegisterResponse
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";
}