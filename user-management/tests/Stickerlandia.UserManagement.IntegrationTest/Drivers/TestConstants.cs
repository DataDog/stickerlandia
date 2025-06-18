namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

internal static class TestConstants
{
    public static string DefaultTestUrl =
        Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "https://localhost:51545";

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
            _ => throw new NotSupportedException($"Unsupported messaging provider: {hostOn}")
        };
    }
}