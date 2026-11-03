namespace FleetTrack.Domain.Entities;

/// <summary>Evento ya procesado por el consumidor. Id = MessageId del outbox, por lo que una repeticion se detecta por clave primaria.</summary>
public class EventLog
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
