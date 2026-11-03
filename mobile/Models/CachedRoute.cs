using SQLite;

namespace FleetTrack.Mobile.Models;

/// <summary>Ultima ruta consultada, guardada localmente para consulta sin conexion.</summary>
public class CachedRoute
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string DriverName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
