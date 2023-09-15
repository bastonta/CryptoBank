using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CryptoBank.WebApi.Features.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CryptoBank.WebApi.Features.Identity.Services;

public class JwtTokenService
{
    private readonly IdentityOptions _identityOptions;

    public JwtTokenService(IOptions<IdentityOptions> options)
    {
        _identityOptions = options.Value;
    }

    public string GenerateToken(Guid userId, string email, Guid refreshTokenId, string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Sid, refreshTokenId.ToString()),
            new(ClaimTypes.Email, email),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_identityOptions.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var token = new JwtSecurityToken(
            _identityOptions.Issuer,
            _identityOptions.Audience,
            claims,
            expires: DateTime.UtcNow.Add(_identityOptions.TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
