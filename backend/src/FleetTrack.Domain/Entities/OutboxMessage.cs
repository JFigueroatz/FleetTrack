namespace FleetTrack.Domain.Entities;

/// <summary>Evento pendiente de publicar a RabbitMQ; se guarda en la misma transaccion que el cambio de negocio (outbox).</summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
