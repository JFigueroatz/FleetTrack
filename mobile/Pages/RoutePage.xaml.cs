using FleetTrack.Mobile.Models;
using FleetTrack.Mobile.Services;

namespace FleetTrack.Mobile.Pages;

public partial class RoutePage : ContentPage
{
    private readonly LocalDatabase _db;
    private readonly SessionState _session;

    public RoutePage(LocalDatabase db, SessionState session)
    {
        InitializeComponent();
        _db = db;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        DriverLabel.Text = _session.DriverName ?? "Conductor";

        // D02: ruta simulada. La ruta real (GET /api/routes/{driverId}) llega en D02 semana 3.
        var route = new CachedRoute
        {
            DriverName = _session.DriverName ?? "Conductor",
            Summary = "1 entrega pendiente - Cliente de Prueba, Zona 1"
        };
        await _db.SaveRouteAsync(route);

        var cached = await _db.GetCachedRouteAsync();
        SummaryLabel.Text = cached?.Summary ?? "Sin ruta en cache.";
        CachedAtLabel.Text = cached is null ? string.Empty : $"Guardada en SQLite: {cached.CachedAt:u}";
    }
}
