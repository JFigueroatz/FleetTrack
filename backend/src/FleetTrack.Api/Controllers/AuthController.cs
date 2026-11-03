using FleetTrack.Api.Auth;
using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FleetTrack.Api.Controllers;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string FullName, IReadOnlyList<string> Roles);

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user, roles);

        return Ok(new LoginResponse(token, user.FullName, roles.ToList()));
    }
}
