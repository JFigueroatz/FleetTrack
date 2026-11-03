using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(FleetTrackDbContext db, IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.Supervisor, Roles.Driver })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        if (await db.Drivers.AnyAsync())
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        var supervisorUser = new AppUser { Id = Guid.NewGuid(), UserName = "supervisor@fleettrack.test", Email = "supervisor@fleettrack.test", FullName = "Supervisor de Prueba", EmailConfirmed = true };
        await userManager.CreateAsync(supervisorUser, "Supervisor123!");
        await userManager.AddToRoleAsync(supervisorUser, Roles.Supervisor);

        var driverUser = new AppUser { Id = Guid.NewGuid(), UserName = "conductor.prueba@fleettrack.test", Email = "conductor.prueba@fleettrack.test", FullName = "Conductor de Prueba", EmailConfirmed = true };
        await userManager.CreateAsync(driverUser, "Conductor123!");
        await userManager.AddToRoleAsync(driverUser, Roles.Driver);

        var driver = new Driver { Id = Guid.NewGuid(), UserId = driverUser.Id, FullName = "Conductor de Prueba", Email = "conductor.prueba@fleettrack.test", Phone = "0000-0000" };
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
