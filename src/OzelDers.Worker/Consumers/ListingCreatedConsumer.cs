using MassTransit;
using Microsoft.Extensions.Logging;
using OzelDers.Business.Events;
using OzelDers.Business.Interfaces;

namespace OzelDers.Worker.Consumers;

public class ListingCreatedConsumer : IConsumer<ListingCreatedEvent>
{
    private readonly ILogger<ListingCreatedConsumer> _logger;
    private readonly IListingService _listingService;
    private readonly ISearchService _searchService;
    private readonly ICacheService _cacheService;

    public ListingCreatedConsumer(
        ILogger<ListingCreatedConsumer> logger, 
        IListingService listingService,
        ISearchService searchService,
        ICacheService cacheService)
    {
        _logger = logger;
        _listingService = listingService;
        _searchService = searchService;
        _cacheService = cacheService;
    }

    public async Task Consume(ConsumeContext<ListingCreatedEvent> context)
    {
        _logger.LogInformation("Processing ListingCreatedEvent for ListingId: {ListingId}", context.Message.ListingId);

        try
        {
            // 1. İlan detaylarını veritabanından getir
            var listing = await _listingService.GetByIdAsync(context.Message.ListingId);
            
            if (listing != null)
            {
                // 2. Elasticsearch'e indexle
                await _searchService.IndexListingAsync(listing);
                _logger.LogInformation("Indexed listing {ListingId} successfully.", listing.Id);

                // 3. İlgili arama cache'lerini temizle
                await _cacheService.RemoveByPatternAsync("search:*");
            }
            else
            {
                _logger.LogWarning("Listing not found for Event: {ListingId}", context.Message.ListingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ListingCreatedEvent for ListingId: {ListingId}", context.Message.ListingId);
            throw; // RabbitMQ'nun yeniden denemesi (retry) için fırlat
        }
    }
}
