using Microsoft.Extensions.Caching.Memory;
using MetaExchangeApi.Services;
using MetaExchangeApi.Models;
using dotenv.net;

public class OrderBookHostedService : IHostedService
{
    private readonly MetaExchangeService _metaExchangeService;
    private readonly ILogger<OrderBookHostedService> _logger;
    private readonly IMemoryCache _memoryCache;

    public OrderBookHostedService(IMemoryCache memoryCache, ILogger<OrderBookHostedService> logger)
    {
        _metaExchangeService = new MetaExchangeService();
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadAndCacheOrderBooksAsync();
    }

    private async Task LoadAndCacheOrderBooksAsync()
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string dataPath = Path.Combine(basePath, "Data", "order_books_data");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            DotEnv.Load();
            dataPath = Environment.GetEnvironmentVariable("RELATIVE_DATA_PATH") ?? dataPath;
        }

        List<OrderBook> orderBooks = await _metaExchangeService.LoadOrderBooksParallel(dataPath);
        List<SortedOrderEntry> sortedBids = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Sell");
        List<SortedOrderEntry> sortedAsks = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Buy");
        Dictionary<string, OrderBookBalances> exchangeBalances = await _metaExchangeService.LoadBalancesAsync(orderBooks);

        _memoryCache.Set("sortedBids", sortedBids);
        _memoryCache.Set("sortedAsks", sortedAsks);
        _memoryCache.Set("exchangeBalances", exchangeBalances);
        //_memoryCache.Set("orderBooks", orderBooks);
        _logger.LogDebug("Order books loaded and cached.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Perform any necessary cleanup here
        return Task.CompletedTask;
    }
}