using System.Text.Json;
using FleetTrack.Domain.Entities;

namespace FleetTrack.Api.Persistence;

public static class OutboxExtensions
{
    /// <summary>Encola el evento en el contexto; se persiste con el mismo SaveChanges que el cambio de negocio.</summary>
    public static void Enqueue<T>(this FleetTrackDbContext db, string type, T payload, DateTimeOffset occurredAt) =>
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = occurredAt
        });
}
