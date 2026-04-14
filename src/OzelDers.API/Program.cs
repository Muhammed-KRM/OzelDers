using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OzelDers.API.Middleware;
using OzelDers.Business;
using Microsoft.AspNetCore.RateLimiting;
using OzelDers.Data;
using OzelDers.Data.Context;
using OzelDers.Data.Seeds;
using OzelDers.Business.Interfaces;
using BC = BCrypt.Net.BCrypt;

var builder = WebApplication.CreateBuilder(args);

// === 1. VERİTABANI ===
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDataLayer(connectionString);

// === 2. İŞ KATMANI ===
builder.Services.AddBusinessServices();

// === 3. MassTransit + RabbitMQ ===
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var mqHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var mqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var mqPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(mqHost, "/", h =>
        {
            h.Username(mqUser);
            h.Password(mqPass);
        });
    });
});

// === 4. JWT KİMLİK DOĞRULAMA ===
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
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
{
    // Development: her yerden izin ver
    options.AddPolicy("AllowAll", b => b
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    // Production: sadece izin verilen origin'ler
    options.AddPolicy("Production", b => b
        .WithOrigins(
            builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? ["https://ozelders.com", "https://www.ozelders.com"])
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// === 6. MassTransit (Event Publishing) ===

var app = builder.Build();

// === MİDDLEWARE PİPELINE ===
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger her ortamda açık (API geliştirme kolaylığı için)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OzelDers.API v1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}
else
{
    app.MapGet("/", () => Results.NotFound());
}

app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");
app.UseStaticFiles(); // uploads klasörü için
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // Middleware kuyruğunda Auth'dan sonra gelmesi uygun
app.MapControllers();

// === VERİTABANI SEED ===
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // PostgreSQL'in hazır olmasını bekle (Docker race condition)
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Veritabanı migration başarılı.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            if (retries == 0) throw;
            logger.LogWarning("Veritabanına bağlanılamadı, {Retries} deneme kaldı. Hata: {Error}", retries, ex.Message);
            await Task.Delay(3000);
        }
    }
    
    await DatabaseSeeder.SeedAsync(context);
    
    // Admin kullanıcı seed — bilgiler appsettings/environment'tan okunur, kaynak koda gömülmez
    var adminEmail = builder.Configuration["AdminSeed:Email"]
        ?? throw new InvalidOperationException("AdminSeed:Email konfigürasyonu eksik.");
    var adminPassword = builder.Configuration["AdminSeed:Password"]
        ?? throw new InvalidOperationException("AdminSeed:Password konfigürasyonu eksik.");

    var existingAdmin = context.Users
        .FirstOrDefault(u => u.Role == OzelDers.Data.Enums.UserRole.Admin);

    if (existingAdmin == null)
    {
        context.Users.Add(new OzelDers.Data.Entities.User
        {
            Email = adminEmail,
            PasswordHash = BC.HashPassword(adminPassword),
            FullName = "Site Yöneticisi",
            Role = OzelDers.Data.Enums.UserRole.Admin,
            IsActive = true,
            IsEmailVerified = true,
            TokenBalance = 0
        });
        await context.SaveChangesAsync();
        logger.LogInformation("Admin kullanıcı oluşturuldu: {Email}", adminEmail);
    }
    else if (existingAdmin.Email != adminEmail || !BC.Verify(adminPassword, existingAdmin.PasswordHash))
    {
        // E-posta veya şifre değişmişse güncelle
        existingAdmin.Email = adminEmail;
        existingAdmin.PasswordHash = BC.HashPassword(adminPassword);
        await context.SaveChangesAsync();
        logger.LogInformation("Admin bilgileri güncellendi: {Email}", adminEmail);
    }

    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
    await settingService.InitializeDefaultsAsync();
}

app.Run();

public partial class Program { }
