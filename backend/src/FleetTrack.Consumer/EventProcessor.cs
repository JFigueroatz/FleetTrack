using System.Text.Json;
using FleetTrack.Domain.Events;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

namespace FleetTrack.Consumer;

/// <summary>Procesa un evento de forma idempotente: un MessageId ya registrado en EventLogs no se vuelve a aplicar.</summary>
public class EventProcessor(NpgsqlDataSource db, IDistributedCache cache, ILogger<EventProcessor> logger)
{
    public async Task<bool> ProcessAsync(Guid messageId, string type, string payload, CancellationToken ct)
    {
        if (await AlreadyProcessedAsync(messageId, ct))
        {
            logger.LogInformation("Evento {Id} ({Type}) repetido, se ignora.", messageId, type);
            return false;
        }

        // Los efectos van antes del registro: si algo falla despues, el reintento los repite y son idempotentes.
        var occurredAt = type switch
        {
            EventTypes.LocationUpdated => await ApplyLocationAsync(Deserialize<LocationUpdated>(payload), ct),
            EventTypes.RouteStarted => Deserialize<RouteStarted>(payload).StartedAt,
            EventTypes.DeliveryStarted => Deserialize<DeliveryStarted>(payload).StartedAt,
            EventTypes.DeliveryCompleted => Deserialize<DeliveryCompleted>(payload).CompletedAt,
            EventTypes.DeliveryFailed => Deserialize<DeliveryFailed>(payload).FailedAt,
            _ => throw new InvalidOperationException($"Tipo de evento desconocido: {type}")
        };

        await using var cmd = db.CreateCommand(
            """INSERT INTO "EventLogs" ("Id","Type","Payload","OccurredAt","ProcessedAt") VALUES ($1,$2,$3,$4,now()) ON CONFLICT ("Id") DO NOTHING""");
        cmd.Parameters.AddWithValue(messageId);
        cmd.Parameters.AddWithValue(type);
        cmd.Parameters.AddWithValue(payload);
        cmd.Parameters.AddWithValue(occurredAt.ToUniversalTime());
        await cmd.ExecuteNonQueryAsync(ct);

        logger.LogInformation("Evento {Id} ({Type}) procesado.", messageId, type);
        return true;
    }

    /// <summary>La API crea el esquema al arrancar; el consumidor espera a que exista para no mandar eventos validos a la cola de fallidos.</summary>
    public async Task WaitForSchemaAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                await using var cmd = db.CreateCommand("""SELECT to_regclass('"EventLogs"') IS NOT NULL""");
                if ((bool)(await cmd.ExecuteScalarAsync(ct))!)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("PostgreSQL no disponible ({Message}).", ex.Message);
            }
            logger.LogInformation("Esperando el esquema de EventLogs...");
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    /// <summary>Un evento atrasado nunca reemplaza una posicion mas reciente ya cacheada.</summary>
    public static bool IsNewer(DateTimeOffset? cached, DateTimeOffset incoming) => cached is null || incoming > cached;

    private async Task<DateTimeOffset> ApplyLocationAsync(LocationUpdated e, CancellationToken ct)
    {
        var key = Topology.LastPositionKey(e.DriverId);
        try
        {
            var current = await cache.GetStringAsync(key, ct);
            var cachedAt = current is null ? (DateTimeOffset?)null : JsonSerializer.Deserialize<LocationUpdated>(current)!.RecordedAt;
            if (IsNewer(cachedAt, e.RecordedAt))
            {
                await cache.SetStringAsync(key, JsonSerializer.Serialize(e),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct);
            }
            else
            {
                logger.LogInformation("Ubicacion atrasada de {Driver} ({At:o}) no reemplaza la cache.", e.DriverId, e.RecordedAt);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // La cache es una proyeccion: si Redis cae el evento igual se registra y el dashboard lee de PostgreSQL.
            logger.LogWarning(ex, "Redis no disponible; se omite la actualizacion de cache.");
        }
        return e.RecordedAt;
    }

    private async Task<bool> AlreadyProcessedAsync(Guid id, CancellationToken ct)
    {
        await using var cmd = db.CreateCommand("""SELECT 1 FROM "EventLogs" WHERE "Id" = $1""");
        cmd.Parameters.AddWithValue(id);
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    private static T Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload) ?? throw new JsonException("Payload vacio.");
}
