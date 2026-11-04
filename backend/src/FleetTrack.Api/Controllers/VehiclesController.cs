using System.Text.Json.Serialization;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record VehicleRequest(string Plate, string Model, [property: JsonRequired] int CapacityKg);
public record VehicleResponse(Guid Id, string Plate, string Model, int CapacityKg);

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = Roles.Supervisor)]
public class VehiclesController(FleetTrackDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> List()
    {
        var vehicles = await db.Vehicles
            .Select(v => new VehicleResponse(v.Id, v.Plate, v.Model, v.CapacityKg))
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create([FromBody] VehicleRequest request)
    {
        if (await db.Vehicles.AnyAsync(v => v.Plate == request.Plate))
        {
            return Conflict("Ya existe un vehiculo con esa placa.");
        }

        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = request.Plate, Model = request.Model, CapacityKg = request.CapacityKg };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var response = new VehicleResponse(vehicle.Id, vehicle.Plate, vehicle.Model, vehicle.CapacityKg);
        return CreatedAtAction(nameof(List), response);
    }
}
