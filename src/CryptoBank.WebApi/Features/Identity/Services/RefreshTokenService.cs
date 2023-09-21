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

    public async Task<RefreshTokenModel> CreateToken(Guid userId)
    {
        return await GenerateRefreshToken(userId);
    }

    public async Task RevokeToken(Guid tokenId)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId);
        refreshToken.Revoked = DateTime.UtcNow;

        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearRefreshToken(CancellationToken token)
    {
        var expireDate = DateTime.UtcNow.Add(-_identityOptions.RefreshTokenRemoveAfter);

        var refreshTokens = await _dbContext.RefreshTokens
            .Where(s => s.Expires < expireDate)
            .ToListAsync(cancellationToken: token);

        _dbContext.RefreshTokens.RemoveRange(refreshTokens);
        await _dbContext.SaveChangesAsync(token);
    }

    public async Task<RefreshTokenModel> UpdateToken(Guid tokenId, string token)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId);
        if (!refreshToken.IsActive || refreshToken.Token != token)
        {
            throw new InvalidOperationException();
        }

        var transaction = await _dbContext.Database.BeginTransactionAsync();
        refreshToken.Token = await GetUniqueToken();
        refreshToken.Updated = DateTime.UtcNow;
        refreshToken.Expires = refreshToken.Updated.Value.Add(_identityOptions.RefreshTokenLifetime);

        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
        return refreshToken;
    }

    public async Task<RefreshTokenValidationResult> ValidateRefreshToken(Guid tokenId, Guid userId, string token)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleAsync(s => s.Id == tokenId);

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

    public Task<bool> ValidateAccessToken(string token, out Guid tokenId, out Guid userId)
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

    private async Task<RefreshTokenModel> GenerateRefreshToken(Guid userId)
    {
        var id = Guid.NewGuid();
        var token = await GetUniqueToken();
        var refreshToken = new RefreshTokenModel
        {
            Id = id,
            UserId = userId,
            Token = token,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(_identityOptions.RefreshTokenLifetime)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken;
    }

    private async Task<string> GetUniqueToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenIsUnique = !await _dbContext.RefreshTokens.AnyAsync(s => s.Token == token);

        if (!tokenIsUnique)
            return await GetUniqueToken();

        return token;
    }
}
