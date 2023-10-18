using System.Net;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Requests;
using CryptoBank.WebApi.Tests.Integration.Fixtures;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoBank.WebApi.Tests.Integration.Features.Account;

public class GetAccountsTests : IClassFixture<WebApplicationTestFixture>, IAsyncLifetime
{
    private readonly WebApplicationTestFixture _appFixture;
    private readonly AsyncServiceScope _scope;

    public GetAccountsTests(WebApplicationTestFixture appFixture)
    {
        _appFixture = appFixture;
        _scope = _appFixture.Factory.Services.CreateAsyncScope();
    }

    [Fact]
    public async Task Should_return_accounts_list()
    {
        // Arrange
        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());

        var accountCount = 5;
        var accounts = new AccountModel[accountCount];

        var dt = DateTime.UtcNow;
        dt = dt.AddTicks(-dt.Ticks % TimeSpan.TicksPerSecond);

        for (int i = 0; i < accountCount; i++)
        {
            accounts[i] = new AccountModel
            {
                UserId = user.Id,
                Currency = new [] { "BTC", "ETH", "DOGE" }[i % 3],
                Number = Guid.NewGuid().ToString(),
                DateOfOpening = dt.AddDays(-i),
                Amount = (decimal)Random.Shared.NextDouble()
            };
        }

        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddRangeAsync(accounts, Create.CancellationToken());
            await s.SaveChangesAsync(Create.CancellationToken());
        });

        // Act
        var response = await client.GetAsJsonAsync<GetAccounts.Response[]>("/account");

        // Assert
        response.Length.Should().Be(accountCount);
        response.Should().BeEquivalentTo(accounts.Select(s => new GetAccounts.Response(s.Number, s.Currency, s.Amount, s.DateOfOpening)));
    }

    [Fact]
    public async Task Should_return_empty_accounts_list()
    {
        // Arrange
        var (client, _) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());

        // Act
        var response = await client.GetAsJsonAsync<GetAccounts.Response[]>("/account");

        // Assert
        response.Length.Should().Be(0);
    }

    [Fact]
    public async Task Should_not_authenticate_if_wrong_jwt()
    {
        // Arrange
        var (client, _) = await _appFixture.HttpClient.CreateWronglyAuthenticatedClient(Create.CancellationToken());

        // Act
        var httpResponse = await client.GetAsync("/account");

        // Assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }
}
