using System.Net;
using System.Net.Http.Json;

namespace FleetTrack.Api.IntegrationTests;

/// <summary>Administración de clientes, vehículos y conductores (solo supervisor).</summary>
[Collection("FleetTrack API")]
public class ManagementTests(ApiFixture api)
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Supervisor_crea_un_cliente_y_aparece_en_el_listado()
    {
        using var supervisor = api.AsSupervisor();
        var name = $"Cliente {Unique()}";

        var created = await supervisor.PostAsJsonAsync("/api/customers", new { name, address = "Zona 1", phone = "5555-0000" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await supervisor.GetFromJsonAsync<List<NamedResponse>>("/api/customers");
        Assert.Contains(list!, c => c.Name == name);
    }

    [Fact]
    public async Task Supervisor_crea_un_vehiculo_y_la_placa_duplicada_devuelve_409()
    {
        using var supervisor = api.AsSupervisor();
        var plate = $"T-{Unique()}";

        var created = await supervisor.PostAsJsonAsync("/api/vehicles", new { plate, model = "Pickup", capacityKg = 800 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await supervisor.PostAsJsonAsync("/api/vehicles", new { plate, model = "Otro", capacityKg = 100 });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var list = await supervisor.GetFromJsonAsync<List<PlateResponse>>("/api/vehicles");
        Assert.Single(list!, v => v.Plate == plate);
    }

    [Fact]
    public async Task Vehiculo_sin_capacidad_devuelve_400_en_vez_de_asumir_cero()
    {
        using var supervisor = api.AsSupervisor();
        var response = await supervisor.PostAsJsonAsync("/api/vehicles", new { plate = $"T-{Unique()}", model = "Pickup" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Supervisor_crea_un_conductor_que_puede_iniciar_sesion_y_el_correo_duplicado_devuelve_409()
    {
        using var supervisor = api.AsSupervisor();
        var email = $"conductor.{Unique()}@fleettrack.test";
        var body = new { fullName = "Conductor Nuevo", email, password = "Conductor123!", phone = "5555-1111", licenseNumber = "L-1" };

        var created = await supervisor.PostAsJsonAsync("/api/drivers", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var login = await api.Client.PostAsJsonAsync("/api/auth/login", new { email, password = "Conductor123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var duplicate = await supervisor.PostAsJsonAsync("/api/drivers", body);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Conductor_con_contrasena_debil_devuelve_400()
    {
        using var supervisor = api.AsSupervisor();
        var response = await supervisor.PostAsJsonAsync("/api/drivers",
            new { fullName = "Debil", email = $"debil.{Unique()}@fleettrack.test", password = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/customers")]
    [InlineData("/api/vehicles")]
    [InlineData("/api/drivers")]
    public async Task Los_listados_de_administracion_requieren_ser_supervisor(string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync(path)).StatusCode);

        using var driver = api.AsDriver();
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync(path)).StatusCode);
    }

    private record NamedResponse(string Name);
    private record PlateResponse(string Plate);
}
