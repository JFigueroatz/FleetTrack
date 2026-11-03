using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FleetTrack.Mobile.Services;

public record DriverProfile(Guid Id, string FullName, string Email);
public record DeliveryDto(Guid Id, Guid CustomerId, string CustomerName, int Sequence, string Status);
public record RouteDto(Guid Id, Guid DriverId, string DriverName, Guid VehicleId, string Status, List<DeliveryDto> Deliveries);

public class RouteService(HttpClient httpClient, SessionState session)
{
    private void AttachToken() =>
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

    public async Task<DriverProfile> GetMyProfileAsync()
    {
        AttachToken();
        var response = await httpClient.GetAsync($"{AppConfig.ApiBaseUrl}/api/drivers/me");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DriverProfile>()
            ?? throw new InvalidOperationException("Perfil de conductor invalido.");
    }

    public async Task<RouteDto?> GetMyRouteAsync(Guid driverId)
    {
        AttachToken();
        var response = await httpClient.GetAsync($"{AppConfig.ApiBaseUrl}/api/routes/{driverId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RouteDto>();
    }

    public async Task StartDeliveryAsync(Guid deliveryId)
    {
        AttachToken();
        var response = await httpClient.PostAsync($"{AppConfig.ApiBaseUrl}/api/deliveries/{deliveryId}/start", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task CompleteDeliveryAsync(Guid deliveryId)
    {
        AttachToken();
        var response = await httpClient.PostAsync($"{AppConfig.ApiBaseUrl}/api/deliveries/{deliveryId}/complete", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task FailDeliveryAsync(Guid deliveryId, string? reason)
    {
        AttachToken();
        var response = await httpClient.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/api/deliveries/{deliveryId}/fail", new { reason });
        response.EnsureSuccessStatusCode();
    }

    public async Task UploadEvidenceAsync(Guid deliveryId, Stream photoStream, string contentType, string fileName)
    {
        AttachToken();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(photoStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await httpClient.PostAsync($"{AppConfig.ApiBaseUrl}/api/deliveries/{deliveryId}/evidence", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendLocationAsync(double latitude, double longitude, Guid eventId, DateTimeOffset recordedAt)
    {
        AttachToken();
        var response = await httpClient.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/api/tracking", new { latitude, longitude, eventId, recordedAt });
        response.EnsureSuccessStatusCode();
    }
}
