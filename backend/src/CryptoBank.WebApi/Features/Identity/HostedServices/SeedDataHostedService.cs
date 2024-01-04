using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Features.Identity.Constants;
using CryptoBank.WebApi.Features.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CryptoBank.WebApi.Features.Identity.HostedServices;

public class SeedDataHostedService : IHostedService
{
    private readonly ILogger<SeedDataHostedService> _logger;
    private readonly IServiceProvider _services;

    public SeedDataHostedService(ILogger<SeedDataHostedService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SeedDataHostedService is starting");

        await using var scope = _services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await dbContext.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        await dbContext.AddRangeAsync(
            new RoleModel
            {
                Id = Guid.Parse("5a325085-140e-4c7c-8199-18bdffa843de"),
                Name = RoleConstants.Administrator
            },
            new RoleModel
            {
                Id = Guid.Parse("c0eafc9a-7e79-48a1-b652-f2f068f725b1"),
                Name = RoleConstants.Analyst
            },
            new RoleModel
            {
                Id = Guid.Parse("d4ed1397-0035-4988-b985-97ad6ff00ef6"),
                Name = RoleConstants.User
            }
        );
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SeedDataHostedService is stopping");
        return Task.CompletedTask;
    }
}
