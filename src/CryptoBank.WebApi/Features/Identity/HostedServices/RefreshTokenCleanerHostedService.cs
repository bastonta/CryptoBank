using CryptoBank.WebApi.Features.Identity.Services;

namespace CryptoBank.WebApi.Features.Identity.HostedServices;

public class RefreshTokenCleanerHostedService : IHostedService, IDisposable
{
    private readonly ILogger<RefreshTokenCleanerHostedService> _logger;
    private readonly IServiceProvider _services;
    private readonly CancellationTokenSource _tokenSource;
    private Timer? _timer;

    public RefreshTokenCleanerHostedService(
        ILogger<RefreshTokenCleanerHostedService> logger,
        IServiceProvider services)
    {
        _logger = logger;
        _services = services;
        _tokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RefreshTokenCleanerHostedService is starting");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RefreshTokenCleanerHostedService is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        _tokenSource.Cancel();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _tokenSource.Dispose();
        _timer?.Dispose();
    }

    private async void DoWork(object? state)
    {
        try
        {
            using var scope = _services.CreateScope();
            var discountService = scope.ServiceProvider.GetRequiredService<RefreshTokenService>();
            await discountService.ClearRefreshToken(_tokenSource.Token);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "RefreshTokenCleanerHostedService failed");
        }
    }
}
