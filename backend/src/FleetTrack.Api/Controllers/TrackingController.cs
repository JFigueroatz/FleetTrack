using System.Security.Claims;
using System.Text.Json;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace FleetTrack.Api.Controllers;

public record TrackingRequest(double Latitude, double Longitude);
public record LastPositionResponse(Guid DriverId, string DriverName, double Latitude, double Longitude, DateTimeOffset RecordedAt);

[ApiController]
[Route("api")]
public class TrackingController(FleetTrackDbContext db, IDistributedCache cache) : ControllerBase
{
    private static string CacheKey(Guid driverId) => $"driver:{driverId}:lastposition";

    /// <summary>API minima de referencia: POST /api/tracking.</summary>
    [HttpPost("tracking")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<IActionResult> Post([FromBody] TrackingRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver is null)
        {
            return NotFound();
        }

        var recordedAt = DateTimeOffset.UtcNow;

        db.TrackingEvents.Add(new TrackingEvent { Id = Guid.NewGuid(), DriverId = driver.Id, Latitude = request.Latitude, Longitude = request.Longitude, RecordedAt = recordedAt });
        await db.SaveChangesAsync();

        var cached = new LastPositionResponse(driver.Id, driver.FullName, request.Latitude, request.Longitude, recordedAt);
        await cache.SetStringAsync(CacheKey(driver.Id), JsonSerializer.Serialize(cached),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

        return Ok();
    }

    /// <summary>Ultima posicion conocida por conductor, para el dashboard. Lee de Redis; si no esta en cache, cae a Postgres.</summary>
    [HttpGet("tracking/latest")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<List<LastPositionResponse>>> Latest()
    {
        var driverIds = await db.Drivers.Select(d => d.Id).ToListAsync();
        var results = new List<LastPositionResponse>();

        foreach (var driverId in driverIds)
        {
            var cached = await cache.GetStringAsync(CacheKey(driverId));
            if (cached is not null)
            {
                results.Add(JsonSerializer.Deserialize<LastPositionResponse>(cached)!);
                continue;
            }

            var last = await db.TrackingEvents
                .Where(t => t.DriverId == driverId)
                .OrderByDescending(t => t.RecordedAt)
                .Include(t => t.Driver)
                .FirstOrDefaultAsync();

            if (last is not null)
            {
                results.Add(new LastPositionResponse(last.DriverId, last.Driver?.FullName ?? string.Empty, last.Latitude, last.Longitude, last.RecordedAt));
            }
        }

        return Ok(results);
    }
}
