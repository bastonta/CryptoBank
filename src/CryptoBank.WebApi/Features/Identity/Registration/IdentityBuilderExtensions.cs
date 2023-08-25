using System.Text;
using CryptoBank.WebApi.Features.Identity.HostedServices;
using CryptoBank.WebApi.Features.Identity.Options;
using CryptoBank.WebApi.Features.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CryptoBank.WebApi.Features.Identity.Registration;

public static class IdentityBuilderExtensions
{
    public static WebApplicationBuilder AddIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<SeedDataHostedService>();

        builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection(IdentityOptions.OptionName));
        builder.Services.AddSingleton<PasswordHasher>();
        builder.Services.AddSingleton<JwtTokenService>();

        var identityOptions = builder.Configuration.GetSection(IdentityOptions.OptionName).Get<IdentityOptions>()!;

        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = identityOptions.Issuer,
                ValidAudience = identityOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identityOptions.JwtKey)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

        return builder;
    }
}
