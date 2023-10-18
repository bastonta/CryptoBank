using System.Security.Claims;
using System.Security.Cryptography;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Exceptions;
using CryptoBank.WebApi.Features.Account.Domain;
using CryptoBank.WebApi.Features.Account.Options;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CryptoBank.WebApi.Features.Account.Requests;

public static class CreateAccount
{
    [HttpPost("/account")]
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
        string Currency
    ) : IRequest;

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Currency).NotEmpty().WithErrorCode("currency_required");
        }
    }

    public class RequestHandler : IRequestHandler<Request>
    {
        private readonly AppDbContext _dbContext;
        private readonly AccountOptions _accountOptions;

        public RequestHandler(AppDbContext dbContext, IOptions<AccountOptions> accountOptions)
        {
            _dbContext = dbContext;
            _accountOptions = accountOptions.Value;
        }

        public async ValueTask<Unit> Handle(Request request, CancellationToken cancellationToken)
        {
            var accountCount = await _dbContext.Accounts.CountAsync(s => s.UserId == request.UserId, cancellationToken: cancellationToken);
            if (accountCount >= _accountOptions.MaxAccountsPerUser)
                throw new LogicConflictException($"You can't have more than {_accountOptions.MaxAccountsPerUser} accounts", "accounts_limit");

            var accountNumber = await GenerateAccountNumber(cancellationToken);
            var account = new AccountModel
            {
                UserId = request.UserId,
                Number = accountNumber,
                Currency = request.Currency,
                Amount = 0,
                DateOfOpening = DateTime.UtcNow
            };

            _dbContext.Accounts.Add(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private async Task<string> GenerateAccountNumber(CancellationToken cancellationToken)
        {
            var bytes = RandomNumberGenerator.GetBytes(16);
            var accountNumber = string.Join("", bytes.Select(s => s % 10));

            var accountNumberExits = await _dbContext.Accounts.AnyAsync(s => s.Number == accountNumber, cancellationToken: cancellationToken);
            if (accountNumberExits)
                return await GenerateAccountNumber(cancellationToken);

            return accountNumber;
        }
    }
}
