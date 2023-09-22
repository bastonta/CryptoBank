using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Enums;
using CryptoBank.WebApi.Features.Identity.Services;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Identity.Requests;

public static class UpdateToken
{
    [AllowAnonymous]
    [HttpPost("/identity/refreshtoken")]
    public class Endpoint : Endpoint<EmptyRequest, ResponseApi>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ResponseApi> ExecuteAsync(EmptyRequest _, CancellationToken ct)
        {
            var refreshToken = HttpContext.Request.Cookies["refreshToken"];
            var result = await _mediator.Send(new Request(RefreshToken: refreshToken), ct);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = result.RefreshTokenExpires,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Path = "/identity/refreshtoken"
            };
            HttpContext.Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

            return new ResponseApi(result.AccessToken);
        }
    }

    public record ResponseApi(
        string AccessToken
    );

    public record Request(
        string? RefreshToken
    ) : IRequest<Response>;

    public record Response(
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpires
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
                user.Locked = DateTime.UtcNow;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync(ct);

                throw new LogicConflictException("Refresh token revoked", "refresh_token_revoked");
            }

            refreshToken = await _refreshTokenService.UpdateToken(refreshToken, ct);
            var accessToken = _tokenService.GenerateToken(user.Id, user.Email, user.Roles.Select(s => s.Name).ToArray());

            return new Response(accessToken, refreshToken.Token, refreshToken.Expires);
        }
    }
}
