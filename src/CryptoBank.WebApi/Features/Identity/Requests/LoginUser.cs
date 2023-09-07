using System.Security;
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
    [HttpPost("/login")]
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
        string Email,
        string Password
    ) : IRequest<Response>;

    public record Response(
        string Token
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

        public RequestHandler(AppDbContext dbContext, PasswordHasher passwordHasher, JwtTokenService tokenService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async ValueTask<Response> Handle(Request request, CancellationToken ct)
        {
            var normalizedEmail = request.Email.NormalizeString();
            var findUser = await _dbContext.Users
                .Include(s => s.Roles)
                .SingleOrDefaultAsync(s => s.NormalizedEmail == normalizedEmail, cancellationToken: ct);

            if (findUser is null)
                throw new NotFoundErrorException("User not found");

            var verifyPassword = _passwordHasher.VerifyHashedPassword(findUser.PasswordHash, request.Password);
            if (!verifyPassword)
                throw new ValidationErrorsException("password", "Invalid password", "invalid_password");

            var token = _tokenService.GenerateToken(findUser.Id, findUser.Email, findUser.Roles.Select(s => s.Name).ToArray());
            return new Response(token);
        }
    }
}
