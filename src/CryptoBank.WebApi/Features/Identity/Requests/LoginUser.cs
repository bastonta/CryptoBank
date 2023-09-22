using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Extensions;
using CryptoBank.WebApi.Features.Identity.Services;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Identity.Requests;

public static class LoginUser
{
    [AllowAnonymous]
    [HttpPost("/identity/login")]
    public class Endpoint : Endpoint<Request, ResponseApi>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ResponseApi> ExecuteAsync(Request request, CancellationToken ct)
        {
            var result = await _mediator.Send(request, ct);

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
        string Email,
        string Password
    ) : IRequest<Response>;

    public record Response(
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpires
    );

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _dbContext;
        private readonly PasswordHasher _passwordHasher;
        private readonly JwtTokenService _tokenService;
        private readonly RefreshTokenService _refreshTokenService;

        public RequestHandler(AppDbContext dbContext, PasswordHasher passwordHasher, JwtTokenService tokenService, RefreshTokenService refreshTokenService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
        }

        public async ValueTask<Response> Handle(Request request, CancellationToken ct)
        {
            var normalizedEmail = request.Email.NormalizeString();
            var findUser = await _dbContext.Users
                .Include(s => s.Roles)
                .SingleOrDefaultAsync(s => s.NormalizedEmail == normalizedEmail, cancellationToken: ct);

            if (findUser is null)
                throw ThrowInvalidCredentials();

            if (findUser.IsLocked)
                throw new LogicConflictException("User locked.", "user_locked");

            var verifyPassword = _passwordHasher.VerifyHashedPassword(findUser.PasswordHash, request.Password);
            if (!verifyPassword)
                throw ThrowInvalidCredentials();

            var refreshToken = await _refreshTokenService.CreateToken(findUser.Id, ct);
            var accessToken = _tokenService.GenerateToken(findUser.Id, findUser.Email, findUser.Roles.Select(s => s.Name).ToArray());
            return new Response(accessToken, refreshToken.Token, refreshToken.Expires);
        }

        private ValidationErrorsException ThrowInvalidCredentials()
        {
            return new ValidationErrorsException("email_or_password", "Invalid credentials", "invalid_credentials");
        }
    }
}
