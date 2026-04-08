using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OzelDers.API.Middleware;
using OzelDers.Business;
using Microsoft.AspNetCore.RateLimiting;
using OzelDers.Data;
using OzelDers.Data.Context;
using OzelDers.Data.Seeds;
using OzelDers.API;

var builder = WebApplication.CreateBuilder(args);

// === 1. VERİTABANI ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDataLayer(connectionString);

// === 2. İŞ KATMANI ===
builder.Services.AddBusinessServices();

// === 3. JWT KİMLİK DOĞRULAMA ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// === 4. SWAGGER ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OzelDers API", Version = "v1" });
});

// === 4.1 RATE LIMITING (Güvenlik) ===
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("SearchPolicy", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("RegisterPolicy", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromHours(1);
    });
});

// === 5. CORS ===
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", b => b
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

// === 6. MassTransit (Event Publishing) ===
// NOTE: MassTransit son sürümlerinde MT_LICENSE istiyor. Şimdilik lokal çalışması için
// Dummy bir IPublishEndpoint kullanıyoruz. (Canlıda RabbitMQ için lisans key girilmelidir).
builder.Services.AddScoped<IPublishEndpoint, DummyPublishEndpoint>();

var app = builder.Build();

// === MİDDLEWARE PİPELINE ===
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OzelDers.API v1");
        c.RoutePrefix = string.Empty; // Swagger'ı ana dizinde (/) göster
    });
}

app.UseCors("AllowAll");
app.UseStaticFiles(); // uploads klasörü için
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Middleware kuyruğunda Auth'dan sonra gelmesi uygun
app.MapControllers();

// === VERİTABANI SEED ===
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context);
}

app.Run();
