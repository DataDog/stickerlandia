using Microsoft.Extensions.DependencyInjection;

namespace Stickerlandia.UserManagement.Lambda;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}