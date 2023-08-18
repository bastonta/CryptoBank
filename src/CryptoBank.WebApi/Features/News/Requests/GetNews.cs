using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.News.Domain;
using CryptoBank.WebApi.Features.News.Options;
using CryptoBank.WebApi.Pipeline;
using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CryptoBank.WebApi.Features.News.Requests;

public static class GetNews
{
    [AllowAnonymous]
    [HttpGet("/news")]
    public class Endpoint : EndpointWithoutRequest<NewsModel[]>
    {
        private readonly Dispatcher _dispatcher;

        public Endpoint(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override async Task<NewsModel[]> ExecuteAsync(CancellationToken cancellationToken) =>
            await _dispatcher.Dispatch(new Request(), cancellationToken);
    }

    public record Request : IRequest<NewsModel[]>;

    public class RequestHandler : IRequestHandler<Request, NewsModel[]>
    {
        private readonly AppDbContext _dbContext;
        private readonly NewsOptions _newsOptions;

        public RequestHandler(AppDbContext dbContext, IOptions<NewsOptions> options)
        {
            _dbContext = dbContext;
            _newsOptions = options.Value;
        }

        public async ValueTask<NewsModel[]> Handle(Request request, CancellationToken cancellationToken)
        {
            return await _dbContext.News
                .OrderByDescending(s => s.Date)
                .Take(_newsOptions.Count)
                .ToArrayAsync(cancellationToken);
        }
    }
}
