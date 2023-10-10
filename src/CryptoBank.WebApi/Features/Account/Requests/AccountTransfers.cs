using System.Security.Claims;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Account.Requests;

public static class AccountTransfers
{
    [HttpPost("/account/transfers")]
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
        [property: FromClaim(ClaimTypes.NameIdentifier)]Guid UserId,
        string FromAccount,
        string ToAccount,
        decimal Amount
    ) : IRequest;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.FromAccount).NotEmpty();
            RuleFor(x => x.ToAccount).NotEmpty().NotEqual(s => s.FromAccount).WithMessage("You can't transfer to the same account");
            RuleFor(x => x.Amount).GreaterThan(0);
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
            var fromAccount = await _dbContext.Accounts.SingleOrDefaultAsync(s => s.Number == request.FromAccount, cancellationToken: cancellationToken);
            if (fromAccount == null)
                throw new ValidationErrorsException("FromAccount", "Account not found", "account_not_found");

            var toAccount = await _dbContext.Accounts.SingleOrDefaultAsync(s => s.Number == request.ToAccount, cancellationToken: cancellationToken);
            if (toAccount == null)
                throw new ValidationErrorsException("ToAccount", "Account not found", "account_not_found");

            if (fromAccount.UserId != request.UserId)
                throw new LogicConflictException("You can't transfer from this account", "invalid_account");

            if (fromAccount.Amount < request.Amount)
                throw new LogicConflictException("You don't have enough money", "not_enough_money");

            fromAccount.Amount -= request.Amount;
            toAccount.Amount += request.Amount;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            _dbContext.Accounts.Update(fromAccount);
            _dbContext.Accounts.Update(toAccount);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
