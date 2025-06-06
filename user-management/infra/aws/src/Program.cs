using Amazon.CDK;

namespace UserManagementService
{
    sealed static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var userManagementServiceStack = new UserManagementServiceStack(app, "UserManagementServiceStack", new StackProps());
            userManagementServiceStack.Tags.SetTag("team", "users");
            userManagementServiceStack.Tags.SetTag("domain", "users");
            app.Synth();
        }
    }
}
