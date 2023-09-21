using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Enums;
using CryptoBank.WebApi.Features.Identity.Services;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Identity.Requests;

public static class UpdateToken
{
    [AllowAnonymous]
    [HttpPost("/identity/refreshtoken")]
    public class Endpoint : Endpoint<Request, Response>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct) =>
            await _mediator.Send(request, ct);
    }

    public record Request(
        string AccessToken,
        string RefreshToken
    ) : IRequest<Response>;

    public record Response(
        string AccessToken,
        string RefreshToken
    );

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty();
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _dbContext;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly JwtTokenService _tokenService;

        public RequestHandler(AppDbContext dbContext, RefreshTokenService refreshTokenService, JwtTokenService tokenService)
        {
            _dbContext = dbContext;
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
        }

        public async ValueTask<Response> Handle(Request request, CancellationToken ct)
        {
            Guid userId;
            Guid tokenId;
            var validAccessToken = await _refreshTokenService.ValidateAccessToken(request.AccessToken, out tokenId, out userId, ct);
            if (!validAccessToken)
                throw new LogicConflictException("Invalid access token", "invalid_access_token");

            var refreshTokenValidationResult = await _refreshTokenService.ValidateRefreshToken(tokenId, userId, request.RefreshToken, ct);
            if (refreshTokenValidationResult == RefreshTokenValidationResult.Expired)
                throw new LogicConflictException("Refresh token expired", "refresh_token_expired");

            var user = await _dbContext.Users.Include(s => s.Roles)
                .SingleAsync(s => s.Id == userId, cancellationToken: ct);

            if (user.IsLocked)
                throw new LogicConflictException("User locked. You can not update refresh token.", "user_locked");

            var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            if (refreshTokenValidationResult == RefreshTokenValidationResult.Revoked)
            {
                if (!user.IsLocked)
                {
                    user.Locked = DateTime.UtcNow;
                    _dbContext.Users.Update(user);
                    await _dbContext.SaveChangesAsync(ct);
                }

                await _refreshTokenService.RevokeToken(tokenId, ct);
                await transaction.CommitAsync(ct);

                throw new LogicConflictException("Refresh token revoked", "refresh_token_revoked");
            }

            var refreshToken = await _refreshTokenService.UpdateToken(tokenId, request.RefreshToken, ct);
            var accessToken =
                _tokenService.GenerateToken(userId, user.Email, tokenId, user.Roles.Select(s => s.Name).ToArray());

            await transaction.CommitAsync(ct);
            return new Response(accessToken, refreshToken.Token);
        }
    }
}
