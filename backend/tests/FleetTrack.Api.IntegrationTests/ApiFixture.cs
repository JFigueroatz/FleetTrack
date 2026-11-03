using System.Net.Http.Json;

namespace FleetTrack.Api.IntegrationTests;

/// <summary>
/// Pruebas de integración reales: requieren `docker compose up -d --build` (API + PostgreSQL + Redis + RabbitMQ) corriendo
/// con el seed de usuarios de prueba. No usan mocks ni una API en memoria: golpean la pila completa por HTTP.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    public HttpClient Client { get; } = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("FLEETTRACK_API_URL") ?? "http://localhost:8080") };

    public string SupervisorToken { get; private set; } = string.Empty;
    public string DriverToken { get; private set; } = string.Empty;
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid CustomerId { get; private set; }

    public async Task InitializeAsync()
    {
        SupervisorToken = await LoginAsync("supervisor@fleettrack.test", "Supervisor123!");
        DriverToken = await LoginAsync("conductor.prueba@fleettrack.test", "Conductor123!");

        using var authed = AsSupervisor();
        DriverId = (await authed.GetFromJsonAsync<List<IdResponse>>("/api/drivers"))!.First().Id;
        VehicleId = (await authed.GetFromJsonAsync<List<IdResponse>>("/api/vehicles"))!.First().Id;
        CustomerId = (await authed.GetFromJsonAsync<List<IdResponse>>("/api/customers"))!.First().Id;
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    public HttpClient AsSupervisor() => WithToken(SupervisorToken);
    public HttpClient AsDriver() => WithToken(DriverToken);

    private HttpClient WithToken(string token)
    {
        var client = new HttpClient { BaseAddress = Client.BaseAddress };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Crea una ruta con N entregas para el conductor de prueba.</summary>
    public async Task<RouteResponse> CreateRouteAsync(int deliveries = 1)
    {
        using var supervisor = AsSupervisor();
        var items = Enumerable.Range(1, deliveries).Select(i => new { customerId = CustomerId, sequence = i });
        var response = await supervisor.PostAsJsonAsync("/api/routes", new { driverId = DriverId, vehicleId = VehicleId, deliveries = items });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RouteResponse>())!;
    }

    private record IdResponse(Guid Id);
    private record LoginResponse(string Token);
}

public record DeliveryItem(Guid Id, Guid CustomerId, int Sequence, string Status);
public record RouteResponse(Guid Id, Guid DriverId, string Status, List<DeliveryItem> Deliveries);

[CollectionDefinition("FleetTrack API")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
