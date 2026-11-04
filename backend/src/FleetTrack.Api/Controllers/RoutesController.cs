using System.Text.Json.Serialization;
using System.Security.Claims;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record CreateRouteDeliveryItem([property: JsonRequired] Guid CustomerId, [property: JsonRequired] int Sequence);
public record CreateRouteRequest([property: JsonRequired] Guid DriverId, [property: JsonRequired] Guid VehicleId, List<CreateRouteDeliveryItem> Deliveries);

public record DeliveryResponse(Guid Id, Guid CustomerId, string CustomerName, int Sequence, string Status);
public record RouteResponse(Guid Id, Guid DriverId, string DriverName, Guid VehicleId, string Status, List<DeliveryResponse> Deliveries);

[ApiController]
[Route("api/routes")]
public class RoutesController(FleetTrackDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<RouteResponse>> Create([FromBody] CreateRouteRequest request)
    {
        if (request.Deliveries.Count == 0)
        {
            return BadRequest("La ruta debe tener al menos una entrega.");
        }

        var driverExists = await db.Drivers.AnyAsync(d => d.Id == request.DriverId);
        var vehicleExists = await db.Vehicles.AnyAsync(v => v.Id == request.VehicleId);
        if (!driverExists || !vehicleExists)
        {
            return BadRequest("Conductor o vehiculo no existe.");
        }

        var route = new DeliveryRoute
        {
            Id = Guid.NewGuid(),
            DriverId = request.DriverId,
            VehicleId = request.VehicleId,
            Status = RouteStatus.Planned,
            Deliveries = request.Deliveries
                .Select(d => new Delivery { Id = Guid.NewGuid(), CustomerId = d.CustomerId, Sequence = d.Sequence, Status = DeliveryStatus.Pending })
                .ToList()
        };

        db.Routes.Add(route);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByDriver), new { driverId = route.DriverId }, await ToResponseAsync(route.Id));
    }

    /// <summary>Ruta activa del conductor. API minima de referencia: GET /api/routes/{driverId}.</summary>
    [HttpGet("{driverId:guid}")]
    [Authorize]
    public async Task<ActionResult<RouteResponse>> GetByDriver(Guid driverId)
    {
        if (User.IsInRole(Roles.Driver) && !await IsOwnDriverAsync(driverId))
        {
            return Forbid();
        }

        var route = await db.Routes
            .Where(r => r.DriverId == driverId && r.Status != RouteStatus.Completed)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (route is null)
        {
            return NotFound();
        }

        return Ok(await ToResponseAsync(route.Id));
    }

    private async Task<bool> IsOwnDriverAsync(Guid driverId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        return await db.Drivers.AnyAsync(d => d.Id == driverId && d.UserId == userId);
    }

    private async Task<RouteResponse> ToResponseAsync(Guid routeId)
    {
        var route = await db.Routes
            .Include(r => r.Driver)
            .Include(r => r.Deliveries).ThenInclude(d => d.Customer)
            .FirstAsync(r => r.Id == routeId);

        return new RouteResponse(
            route.Id,
            route.DriverId,
            route.Driver?.FullName ?? string.Empty,
            route.VehicleId,
            route.Status.ToString(),
            route.Deliveries
                .OrderBy(d => d.Sequence)
                .Select(d => new DeliveryResponse(d.Id, d.CustomerId, d.Customer?.Name ?? string.Empty, d.Sequence, d.Status.ToString()))
                .ToList());
    }
}
