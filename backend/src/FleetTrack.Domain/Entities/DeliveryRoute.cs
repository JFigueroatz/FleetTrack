using FleetTrack.Domain.Enums;

namespace FleetTrack.Domain.Entities;

public class DeliveryRoute
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public RouteStatus Status { get; set; } = RouteStatus.Planned;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
