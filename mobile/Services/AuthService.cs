using System.Net.Http.Json;

namespace FleetTrack.Mobile.Services;

public record LoginRequest(string Email, string Password);
public record LoginResult(string Token, string FullName, List<string> Roles);

public class AuthService(HttpClient httpClient)
{
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/api/auth/login", new LoginRequest(email, password)); // NOSONAR S5332: ver AppConfig

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Correo o contrasena incorrectos."
                : "No se pudo iniciar sesion.");
        }

        return await response.Content.ReadFromJsonAsync<LoginResult>()
            ?? throw new InvalidOperationException("Respuesta de login invalida.");
    }
}
