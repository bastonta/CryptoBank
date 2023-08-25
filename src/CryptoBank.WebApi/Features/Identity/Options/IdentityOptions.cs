namespace CryptoBank.WebApi.Features.Identity.Options;

public class IdentityOptions
{
    public const string OptionName = "Features:Identity";

    public string AdministratorEmail { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string JwtKey { get; set; } = string.Empty;

    public TimeSpan TokenLifetime { get; set; }
}
