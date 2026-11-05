namespace FleetTrack.Mobile.Services;

public static class AppConfig
{
    /// <summary>
    /// 10.0.2.2 es el alias que el emulador de Android usa para llegar al localhost
    /// de la maquina anfitriona. En un dispositivo fisico hay que apuntar a la IP
    /// real del servidor (shortcut: valor fijo, mover a appsettings/MAUI config si
    /// se necesitan varios ambientes).
    /// </summary>
    public const string ApiBaseUrl = "http://10.0.2.2:8080"; // NOSONAR S5332: HTTP solo hacia el emulador (10.0.2.2); la network_security_config no permite cleartext a otro host.
}
