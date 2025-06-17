using Microsoft.Extensions.DependencyInjection;

// Static analysis warnings are disabled for this file, it is temporary
#pragma warning disable CA1822

namespace Stickerlandia.UserManagement.Lambda;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}