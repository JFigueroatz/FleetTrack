using System.Collections.ObjectModel;
using FleetTrack.Mobile.Models;
using FleetTrack.Mobile.Services;

namespace FleetTrack.Mobile.Pages;

public class DeliveryItem
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public partial class RoutePage : ContentPage
{
    private readonly RouteService _routeService;
    private readonly SyncService _sync;
    private readonly LocalDatabase _db;
    private readonly SessionState _session;
    private readonly ObservableCollection<DeliveryItem> _items = [];

    private Guid _driverId;
    private IDispatcherTimer? _locationTimer;

    public RoutePage(RouteService routeService, SyncService sync, LocalDatabase db, SessionState session)
    {
        InitializeComponent();
        _routeService = routeService;
        _sync = sync;
        _db = db;
        _session = session;
        DeliveriesView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        await SyncAsync();
        await LoadRouteAsync();
        StartLocationReporting();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        _locationTimer?.Stop();
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await SyncAsync();
                await LoadRouteAsync();
            });
        }
    }

    /// <summary>Reenvia la cola pendiente y refleja en pantalla cuantas operaciones faltan por confirmar.</summary>
    private async Task<SyncResult> SyncAsync()
    {
        var result = await _sync.FlushAsync();
        PendingLabel.Text = result.Pending == 0
            ? string.Empty
            : $"Sin conexion: {result.Pending} operacion(es) pendiente(s) de sincronizar.";
        if (result.Discarded > 0)
        {
            ErrorLabel.Text = $"{result.Discarded} operacion(es) fueron rechazadas por el servidor y se descartaron.";
        }
        return result;
    }

    private async Task LoadRouteAsync()
    {
        LoadingIndicator.IsRunning = true;
        ErrorLabel.Text = string.Empty;

        try
        {
            var profile = await _routeService.GetMyProfileAsync();
            _driverId = profile.Id;
            DriverLabel.Text = profile.FullName;
            _session.DriverName = profile.FullName;

            var route = await _routeService.GetMyRouteAsync(_driverId);
            if (route is null)
            {
                StatusLabel.Text = "Sin ruta asignada todavia.";
                _items.Clear();
                return;
            }

            StatusLabel.Text = $"Ruta {route.Status} - vehiculo asignado";
            _items.Clear();
            foreach (var delivery in route.Deliveries.OrderBy(d => d.Sequence))
            {
                _items.Add(new DeliveryItem
                {
                    Id = delivery.Id,
                    Label = $"{delivery.Sequence}. {delivery.CustomerName} - {delivery.Status}"
                });
            }

            await _db.SaveRouteAsync(new CachedRoute
            {
                DriverName = profile.FullName,
                Summary = $"{route.Deliveries.Count} entrega(s), estado de ruta: {route.Status}"
            });
        }
        catch (Exception ex)
        {
            var cached = await _db.GetCachedRouteAsync();
            ErrorLabel.Text = cached is null
                ? $"No se pudo cargar la ruta: {ex.Message}"
                : $"Sin conexion. Ultima ruta consultada ({cached.CachedAt.ToLocalTime():t}): {cached.Summary}";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private void StartLocationReporting()
    {
        _locationTimer = Dispatcher.CreateTimer();
        _locationTimer.Interval = TimeSpan.FromSeconds(15);
        _locationTimer.Tick += async (_, _) => await QueueCurrentLocationAsync();
        _locationTimer.Start();
    }

    // La ubicacion se encola siempre (con su hora y EventId) y luego se intenta enviar todo: offline se acumulan, al reconectar salen en orden.
    private async Task QueueCurrentLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (location is not null)
            {
                await _sync.EnqueueAsync(new PendingOperation
                {
                    Kind = OperationKind.Location,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                });
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"No se pudo obtener la ubicacion: {ex.Message}";
        }

        await SyncAsync();
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Guid deliveryId })
        {
            await QueueDeliveryActionAsync(new PendingOperation { Kind = OperationKind.StartDelivery, DeliveryId = deliveryId }, "Entrega iniciada.");
        }
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Guid deliveryId })
        {
            await QueueDeliveryActionAsync(new PendingOperation { Kind = OperationKind.CompleteDelivery, DeliveryId = deliveryId }, "Entrega finalizada.");
        }
    }

    private async void OnFailClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid deliveryId })
        {
            return;
        }

        var reason = await DisplayPromptAsync("Entrega fallida", "Motivo:");
        if (reason is null)
        {
            return;
        }
        await QueueDeliveryActionAsync(
            new PendingOperation { Kind = OperationKind.FailDelivery, DeliveryId = deliveryId, Reason = reason }, "Entrega marcada como fallida.");
    }

    private async void OnEvidenceClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid deliveryId })
        {
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null)
            {
                return;
            }

            // La foto se copia al almacenamiento de la app para poder subirla mas tarde si no hay red.
            var path = Path.Combine(FileSystem.AppDataDirectory, $"evidence-{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}");
            await using (var source = await photo.OpenReadAsync())
            await using (var target = File.Create(path))
            {
                await source.CopyToAsync(target);
            }

            await QueueDeliveryActionAsync(
                new PendingOperation { Kind = OperationKind.UploadEvidence, DeliveryId = deliveryId, FilePath = path, ContentType = "image/jpeg" },
                "Evidencia guardada.");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"No se pudo guardar la evidencia: {ex.Message}";
        }
    }

    private async Task QueueDeliveryActionAsync(PendingOperation operation, string message)
    {
        ErrorLabel.Text = string.Empty;
        await _sync.EnqueueAsync(operation);
        var result = await SyncAsync();
        // Solo se confirma al conductor lo que el servidor ya recibio; si quedo encolada se lo decimos.
        MessageLabel.Text = result.Pending == 0 ? message : $"{message} (guardada, se enviara al reconectar)";
        await LoadRouteAsync();
    }
}
