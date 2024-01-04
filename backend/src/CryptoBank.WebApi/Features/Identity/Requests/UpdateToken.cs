using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Enums;
using CryptoBank.WebApi.Features.Identity.Extensions;
using CryptoBank.WebApi.Features.Identity.Services;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CryptoBank.WebApi.Features.Identity.Requests;

public static class UpdateToken
{
    [AllowAnonymous]
    [HttpPost("/identity/refreshtoken")]
    public class Endpoint : Endpoint<EmptyRequest, Response>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<Response> ExecuteAsync(EmptyRequest _, CancellationToken ct)
        {
            var refreshToken = HttpContext.GetTokenFromCookie();
            var result = await _mediator.Send(new Request(RefreshToken: refreshToken), ct);

            HttpContext.AddTokenToCookie(result.RefreshToken, result.RefreshTokenExpires);
            return result;
        }
    }

    public record Request(
        string? RefreshToken
    ) : IRequest<Response>;

    public record Response(
        string AccessToken,
        [property: JsonIgnore] string RefreshToken,
        [property: JsonIgnore] DateTime RefreshTokenExpires
    );

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
            var refreshToken = await _refreshTokenService.GetToken(request.RefreshToken!, ct);
            if (refreshToken == null)
                throw new LogicConflictException("Refresh token invalid", "refresh_token_invalid");

            var refreshTokenValidationResult = _refreshTokenService.ValidateRefreshToken(refreshToken);
            if (refreshTokenValidationResult == RefreshTokenValidationResult.Expired)
                throw new LogicConflictException("Refresh token expired", "refresh_token_expired");

            var user = await _dbContext.Users.Include(s => s.Roles)
                .SingleAsync(s => s.Id == refreshToken.UserId, cancellationToken: ct);

            if (user.IsLocked)
                throw new LogicConflictException("User locked. You can not update refresh token.", "user_locked");

            if (refreshTokenValidationResult == RefreshTokenValidationResult.Revoked)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                user.Locked = DateTime.UtcNow;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync(ct);

                await _refreshTokenService.RevokeDescendantRefreshTokens(refreshToken, ct);

                await transaction.CommitAsync(ct);

                throw new LogicConflictException("Refresh token revoked", "refresh_token_revoked");
            }

            refreshToken = await _refreshTokenService.RotateRefreshToken(refreshToken, ct);
            var accessToken = _tokenService.GenerateToken(user.Id, user.Email, user.Roles.Select(s => s.Name).ToArray());

            return new Response(accessToken, refreshToken.Token, refreshToken.Expires);
        }
    }
}
