using CryptoBank.WebApi.Features.Identity.Domain;
using CryptoBank.WebApi.Features.News.Domain;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<NewsModel> News { get; set; } = null!;

    public DbSet<UserModel> Users { get; set; } = null!;
    
    public DbSet<RoleModel> Roles { get; set; } = null!;


    public AppDbContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserModel>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<UserModel>()
            .HasIndex(s => s.NormalizedEmail)
            .IsUnique();

        modelBuilder.Entity<RoleModel>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<UserModel>()
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity("UserRoles");
    }
}
