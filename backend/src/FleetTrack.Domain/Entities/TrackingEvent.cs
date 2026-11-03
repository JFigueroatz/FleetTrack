namespace FleetTrack.Domain.Entities;

/// <summary>Historial de ubicaciones; la posicion mas reciente por conductor vive en Redis.</summary>
public class TrackingEvent
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Driver? Driver { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
