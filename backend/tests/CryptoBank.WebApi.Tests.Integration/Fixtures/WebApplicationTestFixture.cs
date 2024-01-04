using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Tests.Integration.Harnesses;
using CryptoBank.WebApi.Tests.Integration.Harnesses.Base;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CryptoBank.WebApi.Tests.Integration.Fixtures;

public class WebApplicationTestFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; }

    public DatabaseHarness<Program, AppDbContext> Database { get; }

    public HttpClientHarness<Program> HttpClient { get; }

    public WebApplicationTestFixture()
    {
        Database = new();
        HttpClient = new(Database);

        Factory = WebApplicationFactoryHelper.Create()
            .AddHarness(Database)
            .AddHarness(HttpClient);
    }

    public async Task InitializeAsync()
    {
        await Database.Start(Factory, Create.CancellationToken());
        await HttpClient.Start(Factory, Create.CancellationToken());

        var _ = Factory.Server;
        await Database.Migrate(Create.CancellationToken());
    }

    public async Task DisposeAsync()
    {
        await HttpClient.Stop(Create.CancellationToken());
        await Database.Stop(Create.CancellationToken());
    }
}
