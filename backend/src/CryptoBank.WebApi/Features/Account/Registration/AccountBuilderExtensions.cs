using CryptoBank.WebApi.Features.Account.Options;

namespace CryptoBank.WebApi.Features.Account.Registration;

public static class AccountBuilderExtensions
{
    public static WebApplicationBuilder AddAccounts(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<AccountOptions>(builder.Configuration.GetSection(AccountOptions.OptionName));
        return builder;
    }
}
