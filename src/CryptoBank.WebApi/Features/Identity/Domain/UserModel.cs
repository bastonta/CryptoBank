namespace CryptoBank.WebApi.Features.Identity.Domain;

public class UserModel
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<RoleModel> Roles { get; set; } = new();
}
