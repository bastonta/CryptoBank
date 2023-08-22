using CryptoBank.WebApi.Features.Identity.HostedServices;
using CryptoBank.WebApi.Features.Identity.Options;
using CryptoBank.WebApi.Features.Identity.Services;

namespace CryptoBank.WebApi.Features.Identity.Registration;

public static class IdentityBuilderExtensions
{
    public static WebApplicationBuilder AddIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<SeedDataHostedService>();

        builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection(IdentityOptions.OptionName));
        builder.Services.AddSingleton<PasswordHasher>();

        return builder;
    }
}
