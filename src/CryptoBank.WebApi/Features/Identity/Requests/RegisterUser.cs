using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Constants;
using CryptoBank.WebApi.Features.Identity.Domain;
using CryptoBank.WebApi.Features.Identity.Extensions;
using CryptoBank.WebApi.Features.Identity.Options;
using CryptoBank.WebApi.Features.Identity.Services;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CryptoBank.WebApi.Features.Identity.Requests;

public static class RegisterUser
{
    [AllowAnonymous]
    [HttpPost("/register")]
    public class Endpoint : Endpoint<Request>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
        {
            await _mediator.Send(request, cancellationToken);
            await SendOkAsync(cancellationToken);
        }
    }

    public record Request(
        string Email,
        string Password,
        DateOnly BirthDate
    ) : IRequest;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Password).MinimumLength(6);
            RuleFor(x => x.BirthDate).NotEmpty();
        }
    }

    public class RequestHandler : IRequestHandler<Request>
    {
        private readonly AppDbContext _dbContext;
        private readonly PasswordHasher _passwordHasher;
        private readonly IdentityOptions _identityOptions;

        public RequestHandler(AppDbContext dbContext, PasswordHasher passwordHasher, IOptions<IdentityOptions> options)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _identityOptions = options.Value;
        }

        public async ValueTask<Unit> Handle(Request request, CancellationToken cancellationToken)
        {
            var normalizedEmail = request.Email.NormalizeString();
            if (await _dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                throw new ValidationErrorsException("email", "User with this email already exists", "email_already_exists");
            }

            var user = new UserModel
            {
                Email = request.Email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                BirthDate = request.BirthDate,
                CreatedAt = DateTime.UtcNow
            };

            if (normalizedEmail == _identityOptions.AdministratorEmail.ToUpperInvariant())
            {
                var administratorRole = await _dbContext.Roles
                    .Include(s => s.Users)
                    .SingleAsync(x => x.Name == RoleConstants.Administrator, cancellationToken);

                if (!administratorRole.Users.Any())
                {
                    user.Roles.Add(administratorRole);
                }
            }

            if (!user.Roles.Any())
            {
                var userRole = await _dbContext.Roles
                    .SingleAsync(x => x.Name == RoleConstants.User, cancellationToken);
                user.Roles.Add(userRole);
            }

            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
