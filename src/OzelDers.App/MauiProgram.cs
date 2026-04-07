using Microsoft.Extensions.Logging;

namespace OzelDers.App;

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
			});

		builder.Services.AddMauiBlazorWebView();
        
        // Dökümandaki gibi "şimdilik direkt Business'ı bağla hız için" seçeneğini kullanıyoruz.
        // İleride HttpClient ile OzelDers.API üzerinden geçilecek.
        var connectionString = "Host=localhost;Port=5432;Database=ozelders;Username=ozelders_user;Password=dev_password";
        OzelDers.Data.ServiceRegistration.AddDataLayer(builder.Services, connectionString);
        OzelDers.Business.DependencyInjection.AddBusinessServices(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
