using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(FleetTrackDbContext db)
    {
        if (await db.Drivers.AnyAsync())
        {
            return;
        }

        var driver = new Driver { Id = Guid.NewGuid(), FullName = "Conductor de Prueba", Email = "conductor.prueba@fleettrack.test", Phone = "0000-0000" };
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = "P-000TEST", Model = "Camion de prueba", CapacityKg = 1000 };
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Cliente de Prueba", Address = "Zona 1, Ciudad de Guatemala", Phone = "0000-0000" };

        var route = new DeliveryRoute
        {
            Id = Guid.NewGuid(),
            DriverId = driver.Id,
            VehicleId = vehicle.Id,
            Status = RouteStatus.Planned,
            Deliveries =
            [
                new Delivery { Id = Guid.NewGuid(), CustomerId = customer.Id, Sequence = 1, Status = DeliveryStatus.Pending }
            ]
        };

        db.Drivers.Add(driver);
        db.Vehicles.Add(vehicle);
        db.Customers.Add(customer);
        db.Routes.Add(route);

        await db.SaveChangesAsync();
    }
}
