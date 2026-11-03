using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FleetTrack.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace FleetTrack.Api.Auth;

public class JwtTokenService(IConfiguration configuration)
{
    public string CreateToken(AppUser user, IEnumerable<string> roles)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Secret.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
