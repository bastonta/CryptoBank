using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Identity.Domain;
using CryptoBank.WebApi.Features.Identity.Enums;
using CryptoBank.WebApi.Features.Identity.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CryptoBank.WebApi.Features.Identity.Services;

public class RefreshTokenService
{
    private readonly AppDbContext _dbContext;
    private readonly IdentityOptions _identityOptions;

    public RefreshTokenService(AppDbContext dbContext, IOptions<IdentityOptions> identityOptions)
    {
        _dbContext = dbContext;
        _identityOptions = identityOptions.Value;
    }

    public async Task<RefreshTokenModel> CreateToken(Guid userId, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var token = await GetUniqueToken(cancellationToken);
        var refreshToken = new RefreshTokenModel
        {
            Id = id,
            UserId = userId,
            Token = token,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(_identityOptions.RefreshTokenLifetime)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task RevokeToken(Guid tokenId, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId, cancellationToken: cancellationToken);
        refreshToken.Revoked = DateTime.UtcNow;

        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearRefreshToken(CancellationToken cancellationToken)
    {
        var expireDate = DateTime.UtcNow.Add(-_identityOptions.RefreshTokenRemoveAfter);

        var refreshTokens = await _dbContext.RefreshTokens
            .Where(s => s.Expires < expireDate)
            .ToListAsync(cancellationToken: cancellationToken);

        _dbContext.RefreshTokens.RemoveRange(refreshTokens);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshTokenModel> UpdateToken(Guid tokenId, string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId, cancellationToken: cancellationToken);
        if (!refreshToken.IsActive || refreshToken.Token != token)
        {
            throw new InvalidOperationException();
        }

        refreshToken.Token = await GetUniqueToken(cancellationToken);
        refreshToken.Updated = DateTime.UtcNow;
        refreshToken.Expires = refreshToken.Updated.Value.Add(_identityOptions.RefreshTokenLifetime);

        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<RefreshTokenValidationResult> ValidateRefreshToken(Guid tokenId, Guid userId, string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId, cancellationToken: cancellationToken);

        if (refreshToken.IsActive && refreshToken.UserId == userId && refreshToken.Token == token)
        {
            return RefreshTokenValidationResult.Success;
        }

        if (refreshToken.IsRevoked)
        {
            return RefreshTokenValidationResult.Revoked;
        }

        if (refreshToken.IsExpired)
        {
            return RefreshTokenValidationResult.Expired;
        }

        return RefreshTokenValidationResult.Revoked;
    }

    public Task<bool> ValidateAccessToken(string token, out Guid tokenId, out Guid userId, CancellationToken cancellationToken)
    {
        tokenId = Guid.Empty;
        userId = Guid.Empty;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_identityOptions.JwtKey);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidAudience = _identityOptions.Audience,
                ValidIssuer = _identityOptions.Issuer,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            if (!Guid.TryParse(jwtToken.Claims.Single(s => s.Type == JwtRegisteredClaimNames.Sid).Value, out tokenId))
            {
                return Task.FromResult(false);
            }

            if (!Guid.TryParse(jwtToken.Claims.Single(s => s.Type == JwtRegisteredClaimNames.Sub).Value, out userId))
            {
                return Task.FromResult(false);
            }
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private async Task<string> GetUniqueToken(CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenIsUnique = !await _dbContext.RefreshTokens.AnyAsync(s => s.Token == token, cancellationToken: cancellationToken);

        if (!tokenIsUnique)
            return await GetUniqueToken(cancellationToken);

        return token;
    }
}
