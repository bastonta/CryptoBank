namespace CryptoBank.WebApi.Features.Identity.Extensions;

public static class StringExtension
{
    public static string NormalizeString(this string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
