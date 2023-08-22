namespace CryptoBank.WebApi.Features.Identity.Options;

public class IdentityOptions
{
    public const string OptionName = "Features:Identity";

    public string AdministratorEmail { get; set; } = string.Empty;
}
