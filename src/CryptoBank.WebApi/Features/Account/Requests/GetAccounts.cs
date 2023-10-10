using System.Security.Claims;
using CryptoBank.WebApi.Data;
using FastEndpoints;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Account.Requests;

public static class GetAccounts
{
    [HttpGet("/account")]
    public class Endpoint : Endpoint<Request, Response[]>
    {
        private readonly IMediator _mediator;

        public Endpoint(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<Response[]> ExecuteAsync(Request req, CancellationToken cancellationToken) =>
            await _mediator.Send(req, cancellationToken);
    }

    public record Request(
        [property: FromClaim(ClaimTypes.NameIdentifier)] Guid UserId
    ) : IRequest<Response[]>;

    public record Response(
        string Number,
        string Currency,
        decimal Amount,
        DateTime DateOfOpening
    );

    public class RequestHandler : IRequestHandler<Request, Response[]>
    {
        private readonly AppDbContext _dbContext;

        public RequestHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Response[]> Handle(Request request, CancellationToken cancellationToken)
        {
            return await _dbContext.Accounts
                .Where(s => s.UserId == request.UserId)
                .Select(s => new Response(s.Number, s.Currency, s.Amount, s.DateOfOpening))
                .ToArrayAsync(cancellationToken);
        }
    }
}
