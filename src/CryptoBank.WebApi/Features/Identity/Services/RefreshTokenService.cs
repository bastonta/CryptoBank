using System.Security.Cryptography;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Identity.Domain;
using CryptoBank.WebApi.Features.Identity.Enums;
using CryptoBank.WebApi.Features.Identity.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    public async Task<RefreshTokenModel?> GetToken(string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(s => s.Token == token, cancellationToken: cancellationToken);
        return  refreshToken;
    }

    public async Task RevokeToken(RefreshTokenModel refreshToken, CancellationToken cancellationToken)
    {
        if (!refreshToken.IsActive)
        {
            throw new InvalidOperationException();
        }
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

    public async Task<RefreshTokenModel> UpdateToken(RefreshTokenModel refreshToken, CancellationToken cancellationToken)
    {
        if (!refreshToken.IsActive)
        {
            throw new InvalidOperationException();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await RevokeToken(refreshToken, cancellationToken);
        var newRefreshToken = await CreateToken(refreshToken.UserId, cancellationToken);

        await  transaction.CommitAsync(cancellationToken);

        return  newRefreshToken;
    }

    public RefreshTokenValidationResult ValidateRefreshToken(RefreshTokenModel refreshToken)
    {
        if (refreshToken.IsActive)
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

    private async Task<string> GetUniqueToken(CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenIsUnique = !await _dbContext.RefreshTokens.AnyAsync(s => s.Token == token, cancellationToken: cancellationToken);

        if (!tokenIsUnique)
            return await GetUniqueToken(cancellationToken);

        return token;
    }
}
