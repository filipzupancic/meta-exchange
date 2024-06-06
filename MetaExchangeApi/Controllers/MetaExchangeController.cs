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

            // Path to the file containing the order books. Hardcoding usually is not a good practice
            // but for the sake of this exercise I makes sense to avoid unnecessary complexity.
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
        // In the real production app we would expose more endpoints for different functionalities
        // like submitting orders, querying order history, balances etc..
        // 
        // @param amount The amount of BTC to buy or sell.
        // @param type The type of the order (Buy or Sell).
        // @return The best paths/prices to buy or sell a given amount of BTC.
        [HttpGet("quote")]
        public async Task<ActionResult<string>> GetQuote(double amount, string type)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // Load and cache order books if they are not already cached this is done only once
            // on the first request. Probably it would make sense to cache it on initialization
            // of the application but for the sake of this exercise we are doing it on the first request.
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

            // Match the order against the order books and find the best paths/prices
            Dictionary<string, double> exchangeToBestPrice = await metaExchangeService.MatchOrderAsync(amount, type, orderBooks);

            watch.Stop();
            _logger.LogDebug($"GetQuote | Execution Time: {watch.ElapsedMilliseconds} ms");

            if (exchangeToBestPrice.Count == 0)
            {
                return BadRequest("No paths found try lower amount.");
            }


            //Initialize a StringBuilder to construct the result string
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Found {exchangeToBestPrice.Count} paths:");

            // Append the best price to the result string
            result.AppendLine($"Best price to {type} {amount} BTC is {exchangeToBestPrice.Values.First()} EUR");

            // Due to ugly printout I decided to change the output to a more readable format
            return result.ToString();
        }

        // This endpoint would be used to submit a trade order. In this case we implemented it
        // to showcase this functionality but in the real production app we would add trade
        // order to the order book or match it against the existing orders depending on order
        // type (limit, market), side (buy, sell) etc..
        [HttpPost("trade")]
        public string SubmitTrade(double amount, string type)
        {
            return $"Your trade to {type} {amount} BTC was successfully submitted";
        }
    }
}
