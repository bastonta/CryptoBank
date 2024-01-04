using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Identity.Constants;
using FastEndpoints;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Account.Requests;

public static class OpenedAccountsReports
{
    [Authorize(Roles = RoleConstants.Analyst)]
    [HttpGet("/account/report")]
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
        DateOnly StartDate,
        DateOnly EndDate
    ) : IRequest<Response[]>;

    public record Response(
        DateOnly Date,
        int AccountCount
    );

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.StartDate).NotEmpty().WithErrorCode("start_date_required")
                .LessThanOrEqualTo(s => s.EndDate).WithErrorCode("start_date_must_less_than_or_equal_end_date")
                .WithMessage("Start date must be less than or equal to end date");
            RuleFor(x => x.EndDate).NotEmpty().WithErrorCode("end_date_required")
                .GreaterThanOrEqualTo(s => s.StartDate).WithErrorCode("end_date_must_greater_than_or_equal_start_date")
                .WithMessage("End date must be greater than or equal to start date");
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response[]>
    {
        private readonly AppDbContext _dbContext;

        public RequestHandler(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<Response[]> Handle(Request request, CancellationToken cancellationToken)
        {
            var startDate = request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endDate = request.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var reports = await _dbContext.Accounts
                .Where(s => startDate <= s.DateOfOpening)
                .Where(s => s.DateOfOpening <= endDate)
                .GroupBy(s => s.DateOfOpening.Date)
                .Select(s => new Response(DateOnly.FromDateTime(s.Key), s.Count()))
                .ToArrayAsync(cancellationToken: cancellationToken);

            return reports;
        }
    }
}
