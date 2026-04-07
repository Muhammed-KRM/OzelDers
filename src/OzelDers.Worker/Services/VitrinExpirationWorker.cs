using MassTransit;
using Microsoft.EntityFrameworkCore;
using OzelDers.Business.Events;
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
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var now = DateTime.UtcNow;

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

                    // ES index'ini güncelle — her biri için ListingUpdatedEvent fırlat
                    foreach (var listing in expiredListings)
                    {
                        await publishEndpoint.Publish(new ListingUpdatedEvent { ListingId = listing.Id }, stoppingToken);
                    }

                    _logger.LogInformation("{Count} vitrin listing(s) expired and ES updated.", expiredListings.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing VitrinExpirationWorker.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
