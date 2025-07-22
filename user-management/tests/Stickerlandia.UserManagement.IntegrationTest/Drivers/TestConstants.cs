namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class TestConstants
{
    public static string DefaultTestUrl =
        Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "https://localhost:51545";

    // OAuth2.0 Configuration
    public static string OAuth2ClientId = "user-authentication";
    public static string OAuth2RedirectUri = "https://localhost:3000/callback";
    public static string[] OAuth2Scopes = ["offline_access"];
    
    // OAuth2.0 Endpoints
    public static string AuthorizeEndpoint = "connect/authorize";
    public static string TokenEndpoint = "connect/token";
    public static string UserInfoEndpoint = "connect/userinfo";

    public static string DefaultMessagingConnection(string hostOn, string? messagingConnectionString = "")
    {
        if (!string.IsNullOrEmpty(messagingConnectionString)) return messagingConnectionString;

        var messagingConnection = Environment.GetEnvironmentVariable("MESSAGING_ENDPOINT");

        if (!string.IsNullOrEmpty(messagingConnection)) return messagingConnection;

        return hostOn switch
        {
            "AZURE" =>
                "Endpoint=sb://localhost:60001;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            "AGNOSTIC" => "localhost:53477",
            "AWS" => "", // SQS does not require a connection string in this context
            _ => throw new NotSupportedException($"Unsupported messaging provider: {hostOn}")
        };
    }
}