using System.ComponentModel.DataAnnotations;

namespace CryptoBank.WebApi.Features.Account.Domain;

public class AccountModel
{
    [Key]
    public string Number { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime DateOfOpening { get; set; }

    public Guid UserId { get; set; }
}
