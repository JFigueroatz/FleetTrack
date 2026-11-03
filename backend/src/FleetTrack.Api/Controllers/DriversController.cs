using System.Security.Claims;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record DriverProfileResponse(Guid Id, string FullName, string Email);
public record DriverRequest(string FullName, string Email, string Password, string? Phone, string? LicenseNumber);

[ApiController]
[Route("api/drivers")]
public class DriversController(FleetTrackDbContext db, UserManager<AppUser> userManager) : ControllerBase
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

    [HttpGet]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<IReadOnlyList<DriverProfileResponse>>> List()
    {
        var drivers = await db.Drivers
            .Select(d => new DriverProfileResponse(d.Id, d.FullName, d.Email))
            .ToListAsync();

        return Ok(drivers);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<DriverProfileResponse>> Create([FromBody] DriverRequest request)
    {
        if (await db.Drivers.AnyAsync(d => d.Email == request.Email))
        {
            return Conflict("Ya existe un conductor con ese correo.");
        }

        var user = new AppUser { Id = Guid.NewGuid(), UserName = request.Email, Email = request.Email, FullName = request.FullName, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, Roles.Driver);

        var driver = new Driver { Id = Guid.NewGuid(), UserId = user.Id, FullName = request.FullName, Email = request.Email, Phone = request.Phone, LicenseNumber = request.LicenseNumber };
        db.Drivers.Add(driver);
        await db.SaveChangesAsync();

        var response = new DriverProfileResponse(driver.Id, driver.FullName, driver.Email);
        return CreatedAtAction(nameof(List), response);
    }
}
