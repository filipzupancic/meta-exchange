using Microsoft.AspNetCore.Mvc;
using MetaExchangeApi.Models;
using MetaExchangeApi.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using dotenv.net;

namespace MetaExchangeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetaExchangeController : ControllerBase
    {
        // Contains logic for loading order books and matching orders
        private readonly MetaExchangeService metaExchangeService = new MetaExchangeService();

        private readonly ILogger<MetaExchangeController> _logger;

        // Memory cache for caching order books
        private readonly IMemoryCache _memoryCache;

        public MetaExchangeController(ILogger<MetaExchangeController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        // Logic for loading and caching order books
        private async Task LoadAndCacheOrderBooksAsync()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            // Path to the file containing the order books. We need to handle the case when the app is running
            // in development mode and the path to the data is different.
            string dataPath = Path.Combine(basePath, "Data", "order_books_data");
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                DotEnv.Load();

                // We could handle potential nullability of RELATIVE_DATA_PATH in a more robust way
                dataPath = Environment.GetEnvironmentVariable("RELATIVE_DATA_PATH")!;
            }

            List<OrderBook> orderBooks = await metaExchangeService.LoadOrderBooksParallel(dataPath);

            _memoryCache.Set("orderBooks", orderBooks);
            _logger.LogDebug("Order books loaded and cached.");
        }

        // GET api/metaexchange/quote
        // This endpoint returns the best price to buy or sell a given amount of BTC.
        // 
        // @param amount The amount of BTC to buy or sell.
        // @param type The type of the order (Buy or Sell).
        // @return The best paths/prices to buy or sell a given amount of BTC.
        [HttpGet("quote")]
        public async Task<ActionResult<string>> GetQuote(double amount, string type)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // Load and cache order books if they are not already cached. We expect this to be done only once
            // on the first request. Probably it would make sense to cache it on initialization
            // of the controller so It doesn't degrade UX. Anyway for the purpose of this task it does not
            // make a big difference as the whole parsing and caching takes less than a second.
            if (!_memoryCache.TryGetValue("orderBooks", out List<OrderBook>? orderBooks))
            {
                await LoadAndCacheOrderBooksAsync();
                orderBooks = _memoryCache.Get<List<OrderBook>>("orderBooks");
            }

            // This should not happen but we need to handle it. Maybe we could handle it more gracefully.
            if (orderBooks == null)
            {
                return BadRequest("Order books not found.");
            }

            // Match the order against the order books and find the best paths/prices.
            Dictionary<string, double> exchangeToBestPrice = await metaExchangeService.MatchOrderAsync(amount, type, orderBooks);

            watch.Stop();
            _logger.LogDebug($"GetQuote | Execution Time: {watch.ElapsedMilliseconds} ms");

            if (exchangeToBestPrice.Count == 0)
            {
                return BadRequest("No paths found try lower amount.");
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Found {exchangeToBestPrice.Count} paths:");
            result.AppendLine($"Best price to {type} {amount} BTC is {exchangeToBestPrice.Values.First()} EUR");
            return result.ToString();
        }

        // This endpoint can be used to submit a trade order. It is not implemented in the current version of the API.
        [HttpPost("trade")]
        public string SubmitTrade(double amount, string type)
        {
            return $"Your trade to {type} {amount} BTC was successfully submitted";
        }
    }
}
