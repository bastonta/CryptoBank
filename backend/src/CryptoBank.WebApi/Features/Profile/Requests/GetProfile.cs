using System.Security.Claims;
using CryptoBank.WebApi.Data;
using FastEndpoints;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Profile.Requests;

public static class GetProfile
{
    [HttpGet("/profile")]
    public class Endpoint : Endpoint<Request, Response>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken) =>
            await _mediator.Send(request, cancellationToken);
    }

    public record Request([property: FromClaim(ClaimTypes.NameIdentifier)]Guid UserId) : IRequest<Response>;

    public record Response(
        Guid Id,
        string Email,
        DateOnly BirthDate
    );

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _dbContext;

        public RequestHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var user = await _dbContext.Users.SingleAsync(s => s.Id == userId, cancellationToken: cancellationToken);
            return new Response(user.Id, user.Email, user.BirthDate);
        }
    }
}
