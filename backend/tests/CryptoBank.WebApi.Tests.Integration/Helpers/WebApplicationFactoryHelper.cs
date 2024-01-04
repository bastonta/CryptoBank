using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace CryptoBank.WebApi.Tests.Integration.Helpers;

public static class WebApplicationFactoryHelper
{
    public static WebApplicationFactory<Program> Create()
    {
        var appsettingPath = Path.Combine(Environment.CurrentDirectory, "appsettings.test.json");
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration(b =>
                {
                    var removeItems = b.Sources.Where(s => s.GetType() == typeof(JsonConfigurationSource)).ToList();
                    foreach (var item in removeItems)
                    {
                        b.Sources.Remove(item);
                    }

                    b.AddJsonFile(appsettingPath);
                })
            );
    }
}
