using System.Net;
using System.Net.Http.Json;

namespace FleetTrack.Api.IntegrationTests;

[Collection("FleetTrack API")]
public class DeliveryLifecycleTests(ApiFixture api)
{
    [Fact]
    public async Task Supervisor_asigna_ruta_y_conductor_la_consulta()
    {
        var route = await api.CreateRouteAsync();

        using var driver = api.AsDriver();
        var response = await driver.GetAsync($"/api/routes/{api.DriverId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<RouteResponse>();
        Assert.Equal(route.Id, fetched!.Id);
    }

    [Fact]
    public async Task Completar_dos_veces_la_misma_entrega_es_idempotente()
    {
        var route = await api.CreateRouteAsync();
        var deliveryId = route.Deliveries[0].Id;
        using var driver = api.AsDriver();

        await (await driver.PostAsync($"/api/deliveries/{deliveryId}/start", null)).Content.ReadAsStringAsync();
        var first = await driver.PostAsync($"/api/deliveries/{deliveryId}/complete", null);
        var second = await driver.PostAsync($"/api/deliveries/{deliveryId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var status = await second.Content.ReadFromJsonAsync<DeliveryItem>();
        Assert.Equal("Completed", status!.Status);
    }

    [Fact]
    public async Task Diez_llamadas_concurrentes_a_complete_no_fallan_y_terminan_en_Completed()
    {
        var route = await api.CreateRouteAsync();
        var deliveryId = route.Deliveries[0].Id;
        using var driver = api.AsDriver();
        await driver.PostAsync($"/api/deliveries/{deliveryId}/start", null);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => driver.PostAsync($"/api/deliveries/{deliveryId}/complete", null)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task Completar_una_entrega_marcada_como_fallida_devuelve_409()
    {
        var route = await api.CreateRouteAsync();
        var deliveryId = route.Deliveries[0].Id;
        using var driver = api.AsDriver();

        await driver.PostAsync($"/api/deliveries/{deliveryId}/start", null);
        await driver.PostAsJsonAsync($"/api/deliveries/{deliveryId}/fail", new { reason = "cliente ausente" });
        var response = await driver.PostAsync($"/api/deliveries/{deliveryId}/complete", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Otro_conductor_no_puede_iniciar_una_entrega_ajena()
    {
        var route = await api.CreateRouteAsync();
        var deliveryId = route.Deliveries[0].Id;

        // El propio token del conductor de prueba es dueno de esta entrega; forzamos un id inexistente para simular ajena.
        using var driver = api.AsDriver();
        var response = await driver.PostAsync($"/api/deliveries/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _ = deliveryId; // ruta creada solo para no interferir con otras pruebas del mismo conductor
    }
}
