using CryptoBank.WebApi.Features.News.Domain;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<NewsModel> News { get; set; } = null!;

    public AppDbContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
}
