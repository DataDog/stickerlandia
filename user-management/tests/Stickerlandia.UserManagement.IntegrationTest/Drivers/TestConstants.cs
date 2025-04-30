namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public static class TestConstants
{
    public static string DefaultTestUrl = Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "http://localhost:7231";

    public static string DefaultMessagingConnection = Environment.GetEnvironmentVariable("MESSAGING_ENDPOINT") ??
        "Endpoint=sb://localhost:7100/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc123";
}