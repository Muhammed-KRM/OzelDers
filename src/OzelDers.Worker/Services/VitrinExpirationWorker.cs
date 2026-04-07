using Microsoft.EntityFrameworkCore;
using OzelDers.Data.Context;

namespace OzelDers.Worker.Services;

public class VitrinExpirationWorker : BackgroundService
{
    private readonly ILogger<VitrinExpirationWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public VitrinExpirationWorker(ILogger<VitrinExpirationWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Vitrin Expiration Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                // Find listings whose Vitrin status has expired
                var expiredListings = await dbContext.Listings
                    .Where(l => l.IsVitrin && l.VitrinExpiresAt.HasValue && l.VitrinExpiresAt.Value <= now)
                    .ToListAsync(stoppingToken);

                if (expiredListings.Any())
                {
                    foreach (var listing in expiredListings)
                    {
                        listing.IsVitrin = false;
                        _logger.LogInformation("Listing {ListingId} vitrin expired.", listing.Id);
                    }

                    dbContext.Listings.UpdateRange(expiredListings);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // Note: We could optionally publish an event here so Elasticsearch updates automatically.
                    // E.g. _publishEndpoint.Publish(new ListingUpdatedEvent { ListingId = ... })
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing VitrinExpirationWorker.");
            }

            // Bekleme süresi: Örneğin her 1 saatte bir kontrol et (Şimdilik test amaçlı 1 dakika verilebilir)
            // Prodüksiyonda 1 saat makuldur.
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
