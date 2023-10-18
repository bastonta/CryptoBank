using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Identity.Domain;
using CryptoBank.WebApi.Features.Identity.Extensions;
using CryptoBank.WebApi.Features.Identity.Options;
using CryptoBank.WebApi.Tests.Integration.Harnesses.Base;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CryptoBank.WebApi.Tests.Integration.Harnesses;

public class HttpClientHarness<TProgram> : IHarness<TProgram>
    where TProgram : class
{
    private readonly DatabaseHarness<TProgram, AppDbContext> _databaseHarness;
    private WebApplicationFactory<TProgram>? _factory;
    private bool _started;

    public HttpClientHarness(DatabaseHarness<TProgram, AppDbContext> databaseHarness)
    {
        _databaseHarness = databaseHarness;
    }

    public void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }

    public Task Start(WebApplicationFactory<TProgram> factory, CancellationToken cancellationToken)
    {
        _factory = factory;
        _started = true;

        return Task.CompletedTask;
    }

    public Task Stop(CancellationToken cancellationToken)
    {
        _started = false;

        return Task.CompletedTask;
    }

    public HttpClient CreateClient()
    {
        ThrowIfNotStarted();

        return _factory!.CreateClient();
    }

    public async Task<(HttpClient, UserModel user)> CreateAuthenticatedClient(CancellationToken cancellationToken)
    {
        ThrowIfNotStarted();

        var email = $"{Guid.NewGuid():N}@test.com";
        var user = new UserModel
        {
            Email = email,
            NormalizedEmail = email.NormalizeString(),
            PasswordHash = Guid.NewGuid().ToString(),
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
        };
        await _databaseHarness.Execute(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        });

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };

        var identityOptions = _factory!.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identityOptions.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            identityOptions.Issuer,
            identityOptions.Audience,
            claims,
            expires: DateTime.UtcNow.Add(identityOptions.TokenLifetime),
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return (client, user);
    }

    public async Task<(HttpClient, UserModel user)> CreateWronglyAuthenticatedClient(CancellationToken cancellationToken)
    {
        ThrowIfNotStarted();

        var email = $"{Guid.NewGuid():N}@test.com";
        var user = new UserModel
        {
            Email = email,
            NormalizedEmail = email.NormalizeString(),
            PasswordHash = Guid.NewGuid().ToString(),
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
        };
        await _databaseHarness.Execute(async context =>
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        });

        var identityOptions = _factory!.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

        var key = new SymmetricSecurityKey("invalidKeyinvalidKeyinvalidKeyinvalidKey"u8.ToArray());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            identityOptions.Issuer,
            identityOptions.Audience,
            expires: DateTime.UtcNow.Add(identityOptions.TokenLifetime),
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var client = _factory!.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return (client, user);
    }

    private void ThrowIfNotStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException($"HTTP client harness is not started. Call {nameof(Start)} first.");
        }
    }
}
