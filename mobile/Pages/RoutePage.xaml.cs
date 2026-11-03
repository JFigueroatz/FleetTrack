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
    private readonly LocalDatabase _db;
    private readonly SessionState _session;
    private readonly ObservableCollection<DeliveryItem> _items = [];

    private Guid _driverId;
    private IDispatcherTimer? _locationTimer;

    public RoutePage(RouteService routeService, LocalDatabase db, SessionState session)
    {
        InitializeComponent();
        _routeService = routeService;
        _db = db;
        _session = session;
        DeliveriesView.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRouteAsync();
        StartLocationReporting();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationTimer?.Stop();
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
            ErrorLabel.Text = $"No se pudo cargar la ruta: {ex.Message}";
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
        _locationTimer.Tick += async (_, _) => await SendCurrentLocationAsync();
        _locationTimer.Start();
    }

    private async Task SendCurrentLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (location is not null)
            {
                await _routeService.SendLocationAsync(location.Latitude, location.Longitude);
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"No se pudo enviar ubicacion: {ex.Message}";
        }
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Guid deliveryId })
        {
            await RunDeliveryActionAsync(() => _routeService.StartDeliveryAsync(deliveryId), "Entrega iniciada.");
        }
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Guid deliveryId })
        {
            await RunDeliveryActionAsync(() => _routeService.CompleteDeliveryAsync(deliveryId), "Entrega finalizada.");
        }
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

            await using var stream = await photo.OpenReadAsync();
            await _routeService.UploadEvidenceAsync(deliveryId, stream, "image/jpeg", photo.FileName);
            MessageLabel.Text = "Evidencia subida.";
            ErrorLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"No se pudo subir la evidencia: {ex.Message}";
        }
    }

    private async Task RunDeliveryActionAsync(Func<Task> action, string successMessage)
    {
        ErrorLabel.Text = string.Empty;
        try
        {
            await action();
            MessageLabel.Text = successMessage;
            await LoadRouteAsync();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"No se pudo completar la accion: {ex.Message}";
        }
    }
}
