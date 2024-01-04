namespace CryptoBank.WebApi.Tests.Integration.Helpers;

public static class Create
{
    public static CancellationToken CancellationToken(int timeoutInSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds)).Token;
}
