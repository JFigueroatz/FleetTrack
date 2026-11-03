using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace FleetTrack.Api.IntegrationTests;

/// <summary>Ejercita API + PostgreSQL + Redis + RabbitMQ (vía el consumidor real) de punta a punta.</summary>
[Collection("FleetTrack API")]
public class TrackingAndBrokerTests(ApiFixture api)
{
    private static readonly string PostgresConnection =
        // Puerto 5433: el 5432 del host puede estar ocupado por un PostgreSQL nativo (ver docker-compose.yml).
        Environment.GetEnvironmentVariable("FLEETTRACK_POSTGRES") ?? "Host=127.0.0.1;Port=5433;Database=fleettrack;Username=fleettrack;Password=dev_pg_pass";

    [Fact]
    public async Task Coordenadas_fuera_de_rango_devuelven_400()
    {
        using var driver = api.AsDriver();
        var response = await driver.PostAsJsonAsync("/api/tracking", new { latitude = 999.0, longitude = 0.0 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reenviar_el_mismo_eventId_de_tracking_no_duplica_la_fila()
    {
        var eventId = Guid.NewGuid();
        using var driver = api.AsDriver();

        await using var db = new NpgsqlConnection(PostgresConnection);
        await db.OpenAsync();
        var before = await CountAsync(db, "select count(*) from \"TrackingEvents\" where \"Id\" = @id", "id", eventId);

        var first = await driver.PostAsJsonAsync("/api/tracking", new { latitude = 14.6, longitude = -90.5, eventId });
        var second = await driver.PostAsJsonAsync("/api/tracking", new { latitude = 14.6, longitude = -90.5, eventId });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var after = await CountAsync(db, "select count(*) from \"TrackingEvents\" where \"Id\" = @id", "id", eventId);
        Assert.Equal(0, before);
        Assert.Equal(1, after);
    }

    [Fact]
    public async Task Una_ubicacion_de_tracking_llega_al_broker_y_el_consumidor_la_registra_en_EventLogs()
    {
        // RecordedAt es el valor que viaja identico dentro del payload del evento (el LocationUpdated no incluye el EventId
        // de la fila de tracking), asi que se usa una marca de tiempo unica para correlacionar lo publicado con lo consumido.
        var recordedAt = DateTimeOffset.UtcNow;
        using var driver = api.AsDriver();
        await driver.PostAsJsonAsync("/api/tracking", new { latitude = 15.0, longitude = -91.0, recordedAt });

        await using var db = new NpgsqlConnection(PostgresConnection);
        await db.OpenAsync();

        var processed = await WaitUntilAsync(async () =>
            await CountAsync(db, """select count(*) from "EventLogs" where "Type" = 'LocationUpdated' and "Payload" like @pattern""", "pattern", $"%{recordedAt:yyyy-MM-ddTHH:mm:ss}%") == 1,
            TimeSpan.FromSeconds(15));

        Assert.True(processed, "El consumidor no registro el evento LocationUpdated a tiempo.");
    }

    private static async Task<long> CountAsync(NpgsqlConnection db, string sql, string paramName, object value)
    {
        await using var cmd = new NpgsqlCommand(sql, db);
        cmd.Parameters.AddWithValue(paramName, value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(500);
        }
        return false;
    }
}
