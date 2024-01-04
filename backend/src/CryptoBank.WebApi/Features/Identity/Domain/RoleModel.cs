namespace CryptoBank.WebApi.Features.Identity.Domain;

public class RoleModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<UserModel> Users { get; set; } = new();
}
