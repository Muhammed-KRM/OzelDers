using MassTransit;
using Microsoft.Extensions.Logging;
using OzelDers.Business.Events;
using OzelDers.Business.Interfaces;

namespace OzelDers.Worker.Consumers;

public class ListingUpdatedConsumer : IConsumer<ListingUpdatedEvent>
{
    private readonly ILogger<ListingUpdatedConsumer> _logger;
    private readonly IListingService _listingService;
    private readonly ISearchService _searchService;
    private readonly ICacheService _cacheService;

    public ListingUpdatedConsumer(
        ILogger<ListingUpdatedConsumer> logger,
        IListingService listingService,
        ISearchService searchService,
        ICacheService cacheService)
    {
        _logger = logger;
        _listingService = listingService;
        _searchService = searchService;
        _cacheService = cacheService;
    }

    public async Task Consume(ConsumeContext<ListingUpdatedEvent> context)
    {
        _logger.LogInformation("Processing ListingUpdatedEvent for ListingId: {ListingId}", context.Message.ListingId);
        try
        {
            var listing = await _listingService.GetByIdAsync(context.Message.ListingId);
            if (listing != null)
            {
                await _searchService.IndexListingAsync(listing);
                await _cacheService.RemoveByPatternAsync("search:*");
                await _cacheService.RemoveAsync($"listing:{listing.Slug}");
                _logger.LogInformation("Updated ES index for listing {ListingId}.", listing.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ListingUpdatedEvent for {ListingId}", context.Message.ListingId);
            throw;
        }
    }
}
