using FleetTrack.Mobile.Services;

namespace FleetTrack.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly SessionState _session;

    public LoginPage(AuthService authService, SessionState session)
    {
        InitializeComponent();
        _authService = authService;
        _session = session;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoginButton.IsEnabled = false;

        try
        {
            var result = await _authService.LoginAsync(EmailEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
            _session.Token = result.Token;
            _session.DriverName = result.FullName;

            await Shell.Current.GoToAsync($"//{nameof(RoutePage)}");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoginButton.IsEnabled = true;
        }
    }
}
