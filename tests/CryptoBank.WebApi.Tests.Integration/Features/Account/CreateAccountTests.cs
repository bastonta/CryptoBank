using System.Net;
using System.Net.Http.Json;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Options;
using CryptoBank.WebApi.Features.Account.Requests;
using CryptoBank.WebApi.Tests.Integration.Errors.Contracts;
using CryptoBank.WebApi.Tests.Integration.Harnesses;
using CryptoBank.WebApi.Tests.Integration.Harnesses.Base;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CryptoBank.WebApi.Tests.Integration.Features.Account;

public class CreateAccountTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    private readonly DatabaseHarness<Program, AppDbContext> _database;
    private readonly HttpClientHarness<Program> _httpClient;

    private AsyncServiceScope _scope;

    public CreateAccountTests()
    {
        _database = new();
        _httpClient = new(_database);

        _factory = WebApplicationFactoryHelper.Create()
            .AddHarness(_database)
            .AddHarness(_httpClient);
    }

    [Fact]
    public async Task Should_create_account()
    {
        // Arrange
        var (client, user) = await _httpClient.CreateAuthenticatedClient(Create.CancellationToken());
        var request = new
        {
            Currency = "BTC",
        };

        // Act
        var response = await client.PostAsync("/account", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var account = await _database.Execute(s => s.Accounts.SingleAsync(x => x.UserId == user.Id));
        account.Should().NotBeNull();
        account.Currency.Should().Be(request.Currency);
        account.UserId.Should().Be(user.Id);
        account.Amount.Should().Be(0);
    }

    [Fact]
    public async Task Should_not_authenticate_if_wrong_jwt()
    {
        // Arrange
        var (client, _) = await _httpClient.CreateWronglyAuthenticatedClient(Create.CancellationToken());
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
        var options = _factory.Services.GetRequiredService<IOptions<AccountOptions>>().Value;

        var (client, user) = await _httpClient.CreateAuthenticatedClient(Create.CancellationToken());

        await _database.Execute(async s =>
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

    public async Task InitializeAsync()
    {
        await _database.Start(_factory, Create.CancellationToken());
        await _httpClient.Start(_factory, Create.CancellationToken());

        var _ = _factory.Server;

        _scope = _factory.Services.CreateAsyncScope();
    }

    public async Task DisposeAsync()
    {
        await _httpClient.Stop(Create.CancellationToken());
        await _database.Stop(Create.CancellationToken());

        await _scope.DisposeAsync();
    }
}

public class CreateAccountValidatorTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly DatabaseHarness<Program, AppDbContext> _database;
    private AsyncServiceScope _scope;
    private CreateAccount.RequestValidator? _validator;

    public CreateAccountValidatorTests()
    {
        _database = new();
        _factory = WebApplicationFactoryHelper.Create()
            .AddHarness(_database);
    }

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

    public async Task InitializeAsync()
    {
        await _database.Start(_factory, Create.CancellationToken());

        _scope = _factory.Services.CreateAsyncScope();
        _validator = new CreateAccount.RequestValidator();
    }

    public async Task DisposeAsync()
    {
        await _database.Stop(Create.CancellationToken());
        await _scope.DisposeAsync();
    }
}
