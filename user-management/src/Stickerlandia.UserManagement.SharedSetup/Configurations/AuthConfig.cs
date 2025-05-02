using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Stickerlandia.UserManagement.Core.Auth;

namespace Stickerlandia.UserManagement.SharedSetup.Configurations;

public static class AuthConfig
{
    public static IServiceCollection AddAuthConfigs(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = configuration["Auth:Issuer"],
                ValidAudience = configuration["Auth:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey
                    (Encoding.UTF8.GetBytes(configuration["Auth:Key"] ?? "")),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true
            };
        });
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("admin", x => x.RequireRole("admin"));
            options.AddPolicy("staff", x => x.RequireRole("staff"));
        });
        
        services.Configure<JwtConfiguration>(configuration.GetSection("Auth"));
        
        return services;
    }
}