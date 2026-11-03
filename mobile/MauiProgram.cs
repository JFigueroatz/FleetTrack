using FleetTrack.Mobile.Pages;
using FleetTrack.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FleetTrack.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<SessionState>();
		builder.Services.AddSingleton(_ =>
			new LocalDatabase(Path.Combine(FileSystem.AppDataDirectory, "fleettrack.db3")));
		builder.Services.AddHttpClient<AuthService>();

		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RoutePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
