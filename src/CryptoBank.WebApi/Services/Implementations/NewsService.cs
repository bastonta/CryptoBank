using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Services.Implementations;

public class NewsService : INewsService
{
    private readonly AppDbContext _dbContext;

    public NewsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<News[]> GetNews(int count, CancellationToken cancellationToken)
    {
        var news = await _dbContext.News
            .OrderByDescending(n => n.Date)
            .Take(count)
            .ToArrayAsync(cancellationToken: cancellationToken);

        return news;
    }
}
