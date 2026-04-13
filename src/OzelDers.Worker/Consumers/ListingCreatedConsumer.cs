using MassTransit;
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
        _logger.LogInformation("ListingCreatedEvent alındı: {ListingId}", context.Message.ListingId);
        try
        {
            var listing = await _listingService.GetByIdAsync(context.Message.ListingId);
            if (listing != null)
            {
                await _searchService.IndexListingAsync(listing);
                await _cacheService.RemoveByPatternAsync("search:*");
                _logger.LogInformation("İlan Elasticsearch'e indexlendi: {ListingId}", listing.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListingCreatedEvent işlenirken hata: {ListingId}", context.Message.ListingId);
            throw;
        }
    }
}
