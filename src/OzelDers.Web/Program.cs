using OzelDers.Business;
using OzelDers.Data;
using OzelDers.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// === 1. API SERVİSLERİ VE BAĞIMLILIKLAR ===
// Web uygulaması veritabanına doğrudan gitmeyip API'ye (HttpClient) yönlendirildi.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001/") });
OzelDers.SharedUI.DependencyInjection.AddSharedApiServices(builder.Services);

// === 2. BLAZOR SERVİSLERİ VE KİMLİK DOĞRULAMA ===
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OzelDersAuth";
        options.LoginPath = "/giris";
        options.AccessDeniedPath = "/giris";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, OzelDers.Web.States.CustomAuthenticationStateProvider>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(OzelDers.SharedUI.Routes).Assembly);

// Dinamik Sitemap.xml Proxy Endpoint'i (Blazor üzerinden erişim)
app.MapGet("/sitemap.xml", async (HttpContext context) => 
{
    // Uyarı: Localhost için HttpClient'ın SSL bypass edilmesi eklendi
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    using var client = new HttpClient(handler);
    
    try
    {
        // Geliştirme ortamında API 5001'de çalıştığı varsayılmıştır
        // Canlıda Nginx ile proxy atılması tavsiye edilir
        var response = await client.GetAsync("https://localhost:5001/api/sitemap");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            context.Response.ContentType = "application/xml";
            await context.Response.WriteAsync(content);
            return;
        }
    }
    catch { /* Hata yutuluyor */ }
    
    context.Response.StatusCode = 500;
});

app.Run();
