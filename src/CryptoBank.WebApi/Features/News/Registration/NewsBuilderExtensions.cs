using CryptoBank.WebApi.Features.News.Options;

namespace CryptoBank.WebApi.Features.News.Registration;

public static class NewsBuilderExtensions
{
    public static WebApplicationBuilder AddNews(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<NewsOptions>(builder.Configuration.GetSection(NewsOptions.OptionName));
        return builder;
    }
}
