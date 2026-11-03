using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Persistence;

public class FleetTrackDbContext(DbContextOptions<FleetTrackDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DeliveryRoute> Routes => Set<DeliveryRoute>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryEvidence> DeliveryEvidences => Set<DeliveryEvidence>();
    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.Plate)
            .IsUnique();

        modelBuilder.Entity<Driver>()
            .HasIndex(d => d.Email)
            .IsUnique();

        modelBuilder.Entity<Driver>()
            .HasIndex(d => d.UserId)
            .IsUnique();

        modelBuilder.Entity<DeliveryRoute>()
            .HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryRoute>()
            .HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.Route)
            .WithMany(r => r.Deliveries)
            .HasForeignKey(d => d.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasIndex(d => new { d.RouteId, d.Sequence })
            .IsUnique();

        modelBuilder.Entity<DeliveryEvidence>()
            .HasOne(e => e.Delivery)
            .WithOne(d => d.Evidence)
            .HasForeignKey<DeliveryEvidence>(e => e.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrackingEvent>()
            .HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrackingEvent>()
            .HasIndex(t => new { t.DriverId, t.RecordedAt });
    }
}
