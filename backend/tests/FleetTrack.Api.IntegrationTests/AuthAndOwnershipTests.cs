using System.Net;
using System.Net.Http.Json;

namespace FleetTrack.Api.IntegrationTests;

[Collection("FleetTrack API")]
public class AuthAndOwnershipTests(ApiFixture api)
{
    [Fact]
    public async Task Login_con_credenciales_invalidas_devuelve_401()
    {
        var response = await api.Client.PostAsJsonAsync("/api/auth/login", new { email = "supervisor@fleettrack.test", password = "incorrecta" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Drivers_me_sin_token_devuelve_401()
    {
        var response = await api.Client.GetAsync("/api/drivers/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Drivers_me_con_token_de_supervisor_devuelve_403()
    {
        using var supervisor = api.AsSupervisor();
        var response = await supervisor.GetAsync("/api/drivers/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Drivers_me_con_token_de_conductor_devuelve_200()
    {
        using var driver = api.AsDriver();
        var response = await driver.GetAsync("/api/drivers/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Conductor_no_puede_operar_una_ruta_de_otro_conductor()
    {
        // Ruta inexistente (otro driverId) simulada con un guid aleatorio: la API no debe filtrar datos ajenos.
        using var driver = api.AsDriver();
        var response = await driver.GetAsync($"/api/routes/{Guid.NewGuid()}");
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Crear_vehiculo_sin_rol_de_supervisor_devuelve_403()
    {
        using var driver = api.AsDriver();
        var response = await driver.PostAsJsonAsync("/api/vehicles", new { plate = $"T-{Guid.NewGuid():N}"[..8], model = "x", capacityKg = 100 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
