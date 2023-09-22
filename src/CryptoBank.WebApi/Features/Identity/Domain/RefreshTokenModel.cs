using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBank.WebApi.Features.Identity.Domain;

public class RefreshTokenModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }

    public DateTime? Revoked { get; set; }

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    [NotMapped]
    public bool IsRevoked => Revoked != null;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
