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
        
        // MAUI Uygulaması: Güvenlik (N-Tier kuralları) gereği DB'yi bilemez. Sadece HttpClient ile API'ye gidecek.
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001/") }); // API Adresi
        
        // MAUI için API Wrapper servislerini bağla
        OzelDers.SharedUI.DependencyInjection.AddSharedApiServices(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
