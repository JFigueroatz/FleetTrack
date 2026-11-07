using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FleetTrack.Api.IntegrationTests;

/// <summary>Integración API↔Redis: la última posición se escribe en la caché al recibir el tracking y se lee de ella.</summary>
[Collection("FleetTrack API")]
public class RedisCacheTests(ApiFixture api)
{
    [Fact]
    public async Task La_ultima_posicion_se_lee_de_Redis_y_no_marca_modo_degradado()
    {
        // Coordenada única por ejecución, para reconocer inequívocamente la posición que acabamos de enviar.
        var latitude = Math.Round(14 + Random.Shared.NextDouble() * 0.9, 5);
        var longitude = Math.Round(-90 - Random.Shared.NextDouble() * 0.9, 5);

        using var driver = api.AsDriver();
        var post = await driver.PostAsJsonAsync("/api/tracking",
            new { latitude, longitude, eventId = Guid.NewGuid(), recordedAt = DateTime.UtcNow });
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);

        using var supervisor = api.AsSupervisor();
        var latest = await supervisor.GetAsync("/api/tracking/latest");
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);

        // Con Redis disponible la API no debe marcar el modo degradado (ver experimento E4 de recuperación).
        Assert.DoesNotContain("unavailable", latest.Headers.TryGetValues("X-Redis-Status", out var values) ? values : []);

        using var body = JsonDocument.Parse(await latest.Content.ReadAsStringAsync());
        Assert.Contains(body.RootElement.EnumerateArray(),
            p => Math.Abs(p.GetProperty("latitude").GetDouble() - latitude) < 1e-6
              && Math.Abs(p.GetProperty("longitude").GetDouble() - longitude) < 1e-6);
    }
}
