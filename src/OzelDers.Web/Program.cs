using OzelDers.Business;
using OzelDers.Data;
using OzelDers.Web.Components;
using OzelDers.Web.States;

var builder = WebApplication.CreateBuilder(args);

// === 1. API SERVİSLERİ VE BAĞIMLILIKLAR ===
var apiBaseAddress = builder.Environment.IsDevelopment() ? "https://localhost:5001/" : "http://api:8080/";

// AuthTokenHandler → HttpClient pipeline'ına enjekte
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseAddress) };
});

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
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, CustomAuthenticationStateProvider>();

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

// Dinamik Sitemap.xml Proxy Endpoint'i
app.MapGet("/sitemap.xml", async (HttpContext context) => 
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    using var client = new HttpClient(handler);
    
    try
    {
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
