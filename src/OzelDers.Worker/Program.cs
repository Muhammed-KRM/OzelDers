using MassTransit;
using OzelDers.Worker.Consumers;
using OzelDers.Business;
using OzelDers.Data;

var builder = Host.CreateApplicationBuilder(args);

// DbContext & Business Services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=ozelders;Username=ozelders_user;Password=dev_password";

OzelDers.Data.ServiceRegistration.AddDataLayer(builder.Services, connectionString);
OzelDers.Business.DependencyInjection.AddBusinessServices(builder.Services);

// RabbitMQ (MassTransit)
builder.Services.AddMassTransit(x =>
{
    // Tüketicileri kaydet
    x.AddConsumer<ListingCreatedConsumer>();
    x.AddConsumer<ListingUpdatedConsumer>();
    x.AddConsumer<ListingDeletedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var mqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
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

// Adım 5.4: Vitrin Süresi Dolum Kontrolcüsü
builder.Services.AddHostedService<OzelDers.Worker.Services.VitrinExpirationWorker>();

var host = builder.Build();
host.Run();
