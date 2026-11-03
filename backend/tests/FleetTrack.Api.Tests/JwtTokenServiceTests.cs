using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FleetTrack.Api.Auth;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace FleetTrack.Api.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-secret-at-least-32-characters-long"
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void CreateToken_IncludesUserIdEmailAndRoleClaims()
    {
        var service = CreateService();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "conductor@fleettrack.test",
            FullName = "Conductor de Prueba"
        };

        var jwt = service.CreateToken(user, [Roles.Driver]);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(Roles.Driver, token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void CreateToken_ThrowsWhenSecretMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new JwtTokenService(config);
        var user = new AppUser { Id = Guid.NewGuid(), Email = "x@x.com", FullName = "X" };

        Assert.Throws<InvalidOperationException>(() => service.CreateToken(user, []));
    }
}
