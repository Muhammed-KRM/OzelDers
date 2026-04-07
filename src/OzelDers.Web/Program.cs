using OzelDers.Business;
using OzelDers.Data;
using OzelDers.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// === 1. BAĞIMLILIKLAR (Döküman Notu: Hız için şimdilik Web tarafına direkt Business bağlanıyor) ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=ozelders;Username=ozelders_user;Password=dev_password";

builder.Services.AddDataLayer(connectionString);
builder.Services.AddBusinessServices();

// === 2. BLAZOR SERVİSLERİ ===
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(OzelDers.SharedUI.Routes).Assembly);

app.Run();
