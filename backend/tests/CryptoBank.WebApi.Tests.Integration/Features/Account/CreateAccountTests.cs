using System.Net;
using System.Net.Http.Json;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Options;
using CryptoBank.WebApi.Features.Account.Requests;
using CryptoBank.WebApi.Tests.Integration.Errors.Contracts;
using CryptoBank.WebApi.Tests.Integration.Fixtures;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Tests.Integration.Features.Account;

public class CreateAccountTests : IClassFixture<WebApplicationTestFixture>, IAsyncLifetime
{
    private readonly WebApplicationTestFixture _appFixture;
    private readonly AsyncServiceScope _scope;

    public CreateAccountTests(WebApplicationTestFixture appFixture)
    {
        _appFixture = appFixture;
        _scope = _appFixture.Factory.Services.CreateAsyncScope();
    }

    [Fact]
    public async Task Should_create_account()
    {
        // Arrange
        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        var request = new
        {
            Currency = "BTC",
        };

        // Act
        var response = await client.PostAsync("/account", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var account = await _appFixture.Database.Execute(s => s.Accounts.SingleAsync(x => x.UserId == user.Id));
        account.Should().NotBeNull();
        account.Currency.Should().Be(request.Currency);
        account.UserId.Should().Be(user.Id);
        account.Amount.Should().Be(0);
    }

    [Fact]
    public async Task Should_not_authenticate_if_wrong_jwt()
    {
        // Arrange
        var (client, _) = await _appFixture.HttpClient.CreateWronglyAuthenticatedClient(Create.CancellationToken());
        var request = new
        {
            Currency = "BTC",
        };

        // Act
        var httpResponse = await client.PostAsync("/account", JsonContent.Create(request));

        // Assert
        httpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_not_create_account_by_limit()
    {
        // Arrange
        var options = _scope.ServiceProvider.GetRequiredService<IOptions<AccountOptions>>().Value;

        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());

        await _appFixture.Database.Execute(async s =>
        {
            for (var i = 0; i < options.MaxAccountsPerUser; i++)
            {
                s.Accounts.Add(new AccountModel
                {
                    UserId = user.Id,
                    Currency = "BTC",
                    Number = Guid.NewGuid().ToString(),
                    DateOfOpening = DateTime.UtcNow
                });
            }

            await s.SaveChangesAsync(Create.CancellationToken());
        });

        var request = new
        {
            Currency = "BTC",
        };

        // Act
        var response = await client.PostAsJsonAsync<LogicConflictProblemDetailsContract>("/account", request, HttpStatusCode.UnprocessableEntity);

        // Assert
        response.LogicConflictShouldContain($"You can't have more than {options.MaxAccountsPerUser} accounts", "accounts_limit");
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

public class CreateAccountValidatorTests
{
    private readonly CreateAccount.RequestValidator? _validator = new();

    [Fact]
    public async Task Should_validate_correct_request()
    {
        var result = await _validator.TestValidateAsync(new CreateAccount.Request(Guid.NewGuid(), "BTC"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_validate_error_currency_empty()
    {
        var result = await _validator.TestValidateAsync(new CreateAccount.Request(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Currency).WithErrorCode("currency_required");
    }
}
