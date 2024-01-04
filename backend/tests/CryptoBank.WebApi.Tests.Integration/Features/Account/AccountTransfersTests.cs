using System.Net;
using System.Net.Http.Json;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Requests;
using CryptoBank.WebApi.Tests.Integration.Errors.Contracts;
using CryptoBank.WebApi.Tests.Integration.Fixtures;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Tests.Integration.Features.Account;

public class AccountTransfersTests : IClassFixture<WebApplicationTestFixture>, IAsyncLifetime
{
    private readonly WebApplicationTestFixture _appFixture;
    private readonly AsyncServiceScope _scope;

    public AccountTransfersTests(WebApplicationTestFixture appFixture)
    {
        _appFixture = appFixture;
        _scope = _appFixture.Factory.Services.CreateAsyncScope();
    }

    [Fact]
    public async Task Should_account_transfer()
    {
        // Arrange
        var fromAccount = Guid.NewGuid().ToString();
        var toAccount = Guid.NewGuid().ToString();
        var notChangeAccount = Guid.NewGuid().ToString();

        var (_, toUser) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = toUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = toAccount,
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = toUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = notChangeAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });

        var (client, fromUser) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = fromUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = fromAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });


        var request = new
        {
            FromAccount = fromAccount,
            ToAccount = toAccount,
            Amount = 3,
        };

        // Act
        var response = await client.PostAsync("/account/transfers", JsonContent.Create(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var accounts = await _appFixture.Database.Execute(s => s.Accounts.ToListAsync());
        accounts.Should().HaveCount(3);

        accounts.Single(s => s.Number == notChangeAccount).Amount.Should().Be(10);
        accounts.Single(s => s.Number == fromAccount).Amount.Should().Be(7);
        accounts.Single(s => s.Number == toAccount).Amount.Should().Be(13);
    }

    [Fact]
    public async Task Should_account_transfer_error_account_not_found_from_account()
    {
        // Arrange
        var fromAccount = Guid.NewGuid().ToString();
        var toAccount = Guid.NewGuid().ToString();

        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = fromAccount,
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = toAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });


        var request = new
        {
            FromAccount = Guid.NewGuid().ToString(),
            ToAccount = toAccount,
            Amount = 3,
        };

        // Act
        var response = await client.PostAsJsonAsync<ValidationErrorProblemDetailsContract>("/account/transfers", request, HttpStatusCode.BadRequest);

        // Assert
        response.Errors.Should().HaveCount(1);
        response.Errors[0].Code.Should().Be("account_not_found");
        response.Errors[0].Field.Should().Be("FromAccount");

        var accounts = await _appFixture.Database.Execute(s => s.Accounts.ToListAsync());
        accounts.Should().HaveCount(2);

        accounts.Single(s => s.Number == fromAccount).Amount.Should().Be(10);
        accounts.Single(s => s.Number == toAccount).Amount.Should().Be(10);
    }

    [Fact]
    public async Task Should_account_transfer_error_account_not_found_to_account()
    {
        // Arrange
        var fromAccount = Guid.NewGuid().ToString();
        var toAccount = Guid.NewGuid().ToString();

        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = fromAccount,
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = toAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });


        var request = new
        {
            FromAccount = fromAccount,
            ToAccount = Guid.NewGuid().ToString(),
            Amount = 3,
        };

        // Act
        var response = await client.PostAsJsonAsync<ValidationErrorProblemDetailsContract>("/account/transfers", request, HttpStatusCode.BadRequest);

        // Assert
        response.Errors.Should().HaveCount(1);
        response.Errors[0].Code.Should().Be("account_not_found");
        response.Errors[0].Field.Should().Be("ToAccount");

        var accounts = await _appFixture.Database.Execute(s => s.Accounts.ToListAsync());
        accounts.Should().HaveCount(2);

        accounts.Single(s => s.Number == fromAccount).Amount.Should().Be(10);
        accounts.Single(s => s.Number == toAccount).Amount.Should().Be(10);
    }

    [Fact]
    public async Task Should_account_transfer_error_invalid_account()
    {
        // Arrange
        var fromAccount = Guid.NewGuid().ToString();
        var toAccount = Guid.NewGuid().ToString();
        var notChangeAccount = Guid.NewGuid().ToString();

        var (_, toUser) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = toUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = toAccount,
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = toUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = notChangeAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });

        var (client, fromUser) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = fromUser.Id,
                Amount = 10,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = fromAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });


        var request = new
        {
            FromAccount = notChangeAccount,
            ToAccount = toAccount,
            Amount = 3,
        };

        // Act
        var response = await client.PostAsJsonAsync<LogicConflictProblemDetailsContract>("/account/transfers", request, HttpStatusCode.UnprocessableEntity);

        // Assert
        response.Code.Should().Be("invalid_account");
        response.Detail.Should().Be("You can't transfer from this account");

        var accounts = await _appFixture.Database.Execute(s => s.Accounts.ToListAsync());
        accounts.Should().HaveCount(3);

        accounts.Single(s => s.Number == notChangeAccount).Amount.Should().Be(10);
        accounts.Single(s => s.Number == fromAccount).Amount.Should().Be(10);
        accounts.Single(s => s.Number == toAccount).Amount.Should().Be(10);
    }

    [Fact]
    public async Task Should_account_transfer_error_not_enough_money()
    {
        // Arrange
        var fromAccount = Guid.NewGuid().ToString();
        var toAccount = Guid.NewGuid().ToString();

        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 0,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = fromAccount,
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 0,
                Currency = "BTC",
                DateOfOpening = DateTime.UtcNow,
                Number = toAccount,
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });


        var request = new
        {
            FromAccount = fromAccount,
            ToAccount = toAccount,
            Amount = 3,
        };

        // Act
        var response = await client.PostAsJsonAsync<LogicConflictProblemDetailsContract>("/account/transfers", request, HttpStatusCode.UnprocessableEntity);

        // Assert
        response.Code.Should().Be("not_enough_money");
        response.Detail.Should().Be("You don't have enough money");

        var accounts = await _appFixture.Database.Execute(s => s.Accounts.ToListAsync());
        accounts.Should().HaveCount(2);

        accounts.Single(s => s.Number == fromAccount).Amount.Should().Be(0);
        accounts.Single(s => s.Number == toAccount).Amount.Should().Be(0);
    }

    public async Task InitializeAsync()
    {
        await _appFixture.Database.Clear(Create.CancellationToken());
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }
}

public class AccountTransfersValidatorTests
{
    private readonly AccountTransfers.RequestValidator? _validator = new();

    [Fact]
    public async Task Should_validate_correct_request()
    {
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_validate_error_authentication_required()
    {
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 1));
        result.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorCode("authentication_required");
    }

    [Fact]
    public async Task Should_validate_error_from_account_required()
    {
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.NewGuid(), string.Empty, Guid.NewGuid().ToString(), 1));
        result.ShouldHaveValidationErrorFor(x => x.FromAccount).WithErrorCode("from_account_required");
    }

    [Fact]
    public async Task Should_validate_error_to_account_required()
    {
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.NewGuid(), Guid.NewGuid().ToString(), string.Empty, 1));
        result.ShouldHaveValidationErrorFor(x => x.ToAccount).WithErrorCode("to_account_required");
    }

    [Fact]
    public async Task Should_validate_error_accounts_same()
    {
        var account = Guid.NewGuid().ToString();
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.NewGuid(), account, account, 1));
        result.ShouldHaveValidationErrorFor(x => x.ToAccount).WithErrorCode("accounts_same");
    }

    [Fact]
    public async Task Should_validate_error_amount_must_greater_than_zero()
    {
        var result = await _validator.TestValidateAsync(new AccountTransfers.Request(
            Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 0));
        result.ShouldHaveValidationErrorFor(x => x.Amount).WithErrorCode("amount_must_greater_than_zero");
    }
}
