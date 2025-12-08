using System.Threading.Channels;

namespace BlazorWebApp.Services;

public class CachePreloader : BackgroundService
{
    private readonly GlobalStatisticsCache _cache;
    private readonly ILogger<CachePreloader> _logger;

    public CachePreloader(GlobalStatisticsCache cache, ILogger<CachePreloader> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting background XML preloading...");
        // Let the Web App start normally first.
        await Task.Delay(3000, stoppingToken);
        try
        {
            await _cache.LoadAllCustomersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during XML preloading.");
        }
        _logger.LogInformation("XML Preloading Finished.");
    }
}
