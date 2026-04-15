using MassTransit;
using OzelDers.Worker.Consumers;
using OzelDers.Business;
using OzelDers.Data;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=ozelders;Username=ozelders_user;Password=dev_password";

OzelDers.Data.ServiceRegistration.AddDataLayer(builder.Services, connectionString);
OzelDers.Business.DependencyInjection.AddBusinessServices(builder.Services);

builder.Services.AddSingleton<OzelDers.Worker.Services.OllamaService>();

// Firebase başlatma
var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
if (!string.IsNullOrEmpty(firebaseCredPath) && File.Exists(firebaseCredPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredPath)
    });
}

// MassTransit v8 — RabbitMQ (ücretsiz, lisans gerektirmez)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ListingCreatedConsumer>();
    x.AddConsumer<ListingUpdatedConsumer>();
    x.AddConsumer<ListingDeletedConsumer>();
    x.AddConsumer<SendNotificationConsumer>();

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

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<OzelDers.Worker.Services.VitrinExpirationWorker>();
builder.Services.AddHostedService<OzelDers.Worker.Services.NotificationCleanupWorker>();

var host = builder.Build();
host.Run();
