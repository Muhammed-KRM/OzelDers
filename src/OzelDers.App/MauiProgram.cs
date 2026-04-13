using Microsoft.Extensions.Logging;

using OzelDers.App.States;
using Microsoft.AspNetCore.Components.Authorization;
using OzelDers.Business.Interfaces;

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
        
        // MAUI Kimlik Doğrulama Servisleri
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthenticationStateProvider>();
        builder.Services.AddScoped<ICustomAuthStateProvider>(sp => (ICustomAuthStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());
        
        // HTTP Client ve Token Yönetimi
        builder.Services.AddScoped<MauiAuthTokenHandler>();
        builder.Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<MauiAuthTokenHandler>();
            handler.InnerHandler = new HttpClientHandler();
            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001/") }; // API Adresi
        });
        
        // MAUI için API Wrapper servislerini bağla
        OzelDers.SharedUI.DependencyInjection.AddSharedApiServices(builder.Services);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
