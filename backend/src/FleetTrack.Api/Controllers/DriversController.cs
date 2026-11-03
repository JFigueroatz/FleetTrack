using System.Security.Claims;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record DriverProfileResponse(Guid Id, string FullName, string Email);

[ApiController]
[Route("api/drivers")]
public class DriversController(FleetTrackDbContext db) : ControllerBase
{
    /// <summary>Perfil del conductor autenticado. Demuestra 401 (sin token), 403 (rol distinto) y 200.</summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Driver)]
    [ProducesResponseType<DriverProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Token sin identificador de usuario."));

        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver is null)
        {
            return NotFound();
        }

        return Ok(new DriverProfileResponse(driver.Id, driver.FullName, driver.Email));
    }
}
