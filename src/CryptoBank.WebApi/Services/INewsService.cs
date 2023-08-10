using CryptoBank.WebApi.Data.Entities;

namespace CryptoBank.WebApi.Services;

public interface INewsService
{
    Task<News[]> GetNews(int count, CancellationToken cancellationToken);
}
