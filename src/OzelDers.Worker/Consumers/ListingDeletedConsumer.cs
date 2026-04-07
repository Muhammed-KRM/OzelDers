using MassTransit;
using Microsoft.Extensions.Logging;
using OzelDers.Business.Events;
using OzelDers.Business.Interfaces;

namespace OzelDers.Worker.Consumers;

public class ListingDeletedConsumer : IConsumer<ListingDeletedEvent>
{
    private readonly ILogger<ListingDeletedConsumer> _logger;
    private readonly ISearchService _searchService;
    private readonly ICacheService _cacheService;

    public ListingDeletedConsumer(
        ILogger<ListingDeletedConsumer> logger,
        ISearchService searchService,
        ICacheService cacheService)
    {
        _logger = logger;
        _searchService = searchService;
        _cacheService = cacheService;
    }

    public async Task Consume(ConsumeContext<ListingDeletedEvent> context)
    {
        _logger.LogInformation("Processing ListingDeletedEvent for ListingId: {ListingId}", context.Message.ListingId);
        try
        {
            await _searchService.DeleteListingIndexAsync(context.Message.ListingId);
            await _cacheService.RemoveByPatternAsync("search:*");
            await _cacheService.RemoveByPatternAsync("vitrin:*");
            _logger.LogInformation("Deleted ES index for listing {ListingId}.", context.Message.ListingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ListingDeletedEvent for {ListingId}", context.Message.ListingId);
            throw;
        }
    }
}
