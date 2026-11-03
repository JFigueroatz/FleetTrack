using System.Security.Claims;
using System.Text.Json;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace FleetTrack.Api.Controllers;

/// <summary>EventId y RecordedAt los envia el movil al reenviar ubicaciones acumuladas offline; sin EventId se genera uno.</summary>
public record TrackingRequest(double Latitude, double Longitude, Guid? EventId = null, DateTimeOffset? RecordedAt = null);
public record LastPositionResponse(Guid DriverId, string DriverName, double Latitude, double Longitude, DateTimeOffset RecordedAt);

[ApiController]
[Route("api")]
public class TrackingController(FleetTrackDbContext db, IDistributedCache cache, ILogger<TrackingController> logger) : ControllerBase
{
    /// <summary>API minima de referencia: POST /api/tracking. Idempotente por EventId.</summary>
    [HttpPost("tracking")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<IActionResult> Post([FromBody] TrackingRequest request)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            return BadRequest("Coordenadas fuera de rango.");
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver is null)
        {
            return NotFound();
        }

        var eventId = request.EventId ?? Guid.NewGuid();
        if (await db.TrackingEvents.AnyAsync(t => t.Id == eventId))
        {
            return Ok();
        }

        // Se acepta la hora del dispositivo (reenvio offline) salvo que venga del futuro.
        var now = DateTimeOffset.UtcNow;
        var recordedAt = request.RecordedAt is { } r && r <= now.AddMinutes(5) ? r : now;

        db.TrackingEvents.Add(new TrackingEvent { Id = eventId, DriverId = driver.Id, Latitude = request.Latitude, Longitude = request.Longitude, RecordedAt = recordedAt });
        db.Enqueue(EventTypes.LocationUpdated, new LocationUpdated(driver.Id, driver.FullName, request.Latitude, request.Longitude, recordedAt), recordedAt);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException) when (request.EventId is not null)
        {
            // Reenvio concurrente del mismo EventId: si ya existe, es exito; si no, era otro error.
            db.ChangeTracker.Clear();
            if (!await db.TrackingEvents.AnyAsync(t => t.Id == eventId))
            {
                throw;
            }
            return Ok();
        }

        await TryCacheAsync(new LastPositionResponse(driver.Id, driver.FullName, request.Latitude, request.Longitude, recordedAt));
        return Ok();
    }

    /// <summary>Ultima posicion conocida por conductor, para el dashboard. Lee de Redis; si no esta en cache o Redis cae, usa Postgres.</summary>
    [HttpGet("tracking/latest")]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<List<LastPositionResponse>>> Latest()
    {
        var driverIds = await db.Drivers.Select(d => d.Id).ToListAsync();
        var results = new List<LastPositionResponse>();
        var redisDown = false;

        foreach (var driverId in driverIds)
        {
            if (!redisDown)
            {
                try
                {
                    var cached = await cache.GetStringAsync(Topology.LastPositionKey(driverId));
                    if (cached is not null)
                    {
                        results.Add(JsonSerializer.Deserialize<LastPositionResponse>(cached)!);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Redis no disponible; el dashboard lee de PostgreSQL.");
                    redisDown = true; // no reintentar por cada conductor
                }
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

        if (redisDown)
        {
            Response.Headers["X-Redis-Status"] = "unavailable";
        }
        return Ok(results);
    }

    /// <summary>Cache de mejor esfuerzo: nunca pisa una posicion mas reciente y no tumba la peticion si Redis falla.</summary>
    private async Task TryCacheAsync(LastPositionResponse position)
    {
        var key = Topology.LastPositionKey(position.DriverId);
        try
        {
            var current = await cache.GetStringAsync(key);
            if (current is not null && JsonSerializer.Deserialize<LastPositionResponse>(current)!.RecordedAt >= position.RecordedAt)
            {
                return;
            }

            await cache.SetStringAsync(key, JsonSerializer.Serialize(position),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis no disponible; la posicion queda solo en PostgreSQL.");
        }
    }
}
