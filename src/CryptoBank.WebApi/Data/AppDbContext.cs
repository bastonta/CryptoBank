using CryptoBank.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<News> News { get; set; } = null!;

    public AppDbContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
}
