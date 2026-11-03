using FleetTrack.Domain.Enums;

namespace FleetTrack.Domain.Entities;

public class Delivery
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public DeliveryRoute? Route { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Orden de la entrega dentro de la ruta.</summary>
    public int Sequence { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public DeliveryEvidence? Evidence { get; set; }
}
