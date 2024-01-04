using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBank.WebApi.Features.Identity.Domain;

public class UserModel
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? Locked { get; set; }

    [NotMapped]
    public bool IsLocked => Locked != null;

    public List<RoleModel> Roles { get; set; } = new();
}
