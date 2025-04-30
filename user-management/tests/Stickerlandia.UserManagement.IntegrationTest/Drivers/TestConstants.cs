namespace Stickerlandia.UserManagement.IntegrationTest.Drivers;

public static class TestConstants
{
    public static string DefaultTestUrl = Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "http://localhost:7261";

    public static string DefaultMessagingConnection = Environment.GetEnvironmentVariable("MESSAGING_ENDPOINT") ??
        "Endpoint=sb://localhost:58041;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
}