using System.Net;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Requests;
using CryptoBank.WebApi.Features.Identity.Constants;
using CryptoBank.WebApi.Tests.Integration.Fixtures;
using CryptoBank.WebApi.Tests.Integration.Helpers;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoBank.WebApi.Tests.Integration.Features.Account;

public class OpenedAccountsReportsTests : IClassFixture<WebApplicationTestFixture>, IAsyncLifetime
{
    private readonly WebApplicationTestFixture _appFixture;
    private readonly AsyncServiceScope _scope;

    public OpenedAccountsReportsTests(WebApplicationTestFixture appFixture)
    {
        _appFixture = appFixture;
        _scope = _appFixture.Factory.Services.CreateAsyncScope();
    }

    [Fact]
    public async Task Should_return_reports()
    {
        var (_, otherUser) = await _appFixture.HttpClient.CreateAuthenticatedClient(Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = otherUser.Id,
                Amount = 0,
                Currency = "BTC",
                DateOfOpening = DateTime.Parse("2021-01-01T10:00:00Z").ToUniversalTime(),
                Number = Guid.NewGuid().ToString(),
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });

        var (client, user) = await _appFixture.HttpClient.CreateAuthenticatedClient(new [] { RoleConstants.Analyst }, Create.CancellationToken());
        await _appFixture.Database.Execute(async s =>
        {
            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 0,
                Currency = "BTC",
                DateOfOpening = DateTime.Parse("2021-01-01T10:00:00Z").ToUniversalTime(),
                Number = Guid.NewGuid().ToString(),
            }, Create.CancellationToken());

            await s.Accounts.AddAsync(new AccountModel
            {
                UserId = user.Id,
                Amount = 0,
                Currency = "BTC",
                DateOfOpening = DateTime.Parse("2021-01-02T10:00:00Z").ToUniversalTime(),
                Number = Guid.NewGuid().ToString(),
            }, Create.CancellationToken());

            await s.SaveChangesAsync(Create.CancellationToken());
        });

        var response = await client.GetAsJsonAsync<OpenedAccountsReports.Response[]>(
            "/account/report?startDate=2021-01-01&endDate=2021-01-02");
        response.Should().HaveCount(2);
        response.Should().Contain(s => s.Date == DateOnly.Parse("2021-01-01") && s.AccountCount == 2);
        response.Should().Contain(s => s.Date == DateOnly.Parse("2021-01-02") && s.AccountCount == 1);
    }

    [Fact]
    public async Task Should_return_empty()
    {
        var (client, _) = await _appFixture.HttpClient.CreateAuthenticatedClient(new [] { RoleConstants.Analyst }, Create.CancellationToken());
        var response = await client.GetAsJsonAsync<OpenedAccountsReports.Response[]>(
            "/account/report?startDate=2021-01-01&endDate=2021-01-02");
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_return_forbidden()
    {
        var (client, _) = await _appFixture.HttpClient.CreateAuthenticatedClient(new [] { RoleConstants.User }, Create.CancellationToken());
        var response = await client.GetAsync("/account/report?startDate=2021-01-01&endDate=2021-01-02");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

public class OpenedAccountsReportsValidatorTests
{
    private readonly OpenedAccountsReports.RequestValidator? _validator = new();

    [Fact]
    public async Task Should_validate_correct_request()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.Parse("2021-01-01"), DateOnly.Parse("2021-01-02")));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_validate_correct_equal_date_request()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.Parse("2021-01-01"), DateOnly.Parse("2021-01-01")));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_validate_error_start_date_required()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.MinValue, DateOnly.Parse("2021-01-02")));
        result.ShouldHaveValidationErrorFor(x => x.StartDate).WithErrorCode("start_date_required");
    }

    [Fact]
    public async Task Should_validate_error_end_date_required()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.Parse("2021-01-01"), DateOnly.MinValue));
        result.ShouldHaveValidationErrorFor(x => x.EndDate).WithErrorCode("end_date_required");
    }

    [Fact]
    public async Task Should_validate_error_start_and_end_date_required()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.MinValue, DateOnly.MinValue));
        result.ShouldHaveValidationErrorFor(x => x.StartDate).WithErrorCode("start_date_required");
        result.ShouldHaveValidationErrorFor(x => x.EndDate).WithErrorCode("end_date_required");
    }

    [Fact]
    public async Task Should_validate_error_start_and_end_date_must_less_than_or_equal_end_date()
    {
        var result = await _validator.TestValidateAsync(
            new OpenedAccountsReports.Request(DateOnly.Parse("2021-01-03"), DateOnly.Parse("2021-01-02")));
        result.ShouldHaveValidationErrorFor(x => x.StartDate).WithErrorCode("start_date_must_less_than_or_equal_end_date");
        result.ShouldHaveValidationErrorFor(x => x.EndDate).WithErrorCode("end_date_must_greater_than_or_equal_start_date");
    }
}
