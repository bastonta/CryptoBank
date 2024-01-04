using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Identity.Constants;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.User.Requests;

public static class UpdateUserRole
{
    [Authorize(Roles = RoleConstants.Administrator)]
    [HttpPut("/user/role")]
    public class Endpoint : Endpoint<Request>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task HandleAsync(Request request, CancellationToken ct)
        {
            await _mediator.Send(request, ct);
        }
    }

    public record Request(
        Guid UserId,
        Guid[] RoleIds
    ) : IRequest;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RoleIds).NotEmpty();
        }
    }

    public class RequestHandler : IRequestHandler<Request>
    {
        private readonly AppDbContext _dbContext;

        public RequestHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Unit> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .Include(s => s.Roles)
                .SingleOrDefaultAsync(s => s.Id == request.UserId, cancellationToken: cancellationToken);

            if (user == null)
            {
                throw new ValidationErrorsException("userId", "User with this ID not found", "user_not_found");
            }

            var roles = await _dbContext.Roles
                .Where(s => request.RoleIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            user.Roles.Clear();
            user.Roles.AddRange(roles);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
