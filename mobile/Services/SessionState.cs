namespace FleetTrack.Mobile.Services;

/// <summary>Sesion del conductor en memoria (shortcut: no persiste entre reinicios; D03 la movera a SQLite).</summary>
public class SessionState
{
    public string? Token { get; set; }
    public string? DriverName { get; set; }
}
