namespace Stickerlandia.UserManagement.AspNet;

public class JwtConfiguration
{
    public string Issuer { get; set; } = "";
    
    public string Audience { get; set; } = "";
    
    public string Key { get; set; } = "";
}