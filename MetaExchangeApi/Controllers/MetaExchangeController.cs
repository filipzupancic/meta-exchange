using Microsoft.AspNetCore.Mvc;
using MetaExchangeApi.Models;
using MetaExchangeApi.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace MetaExchangeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetaExchangeController : ControllerBase
    {
        private readonly MetaExchangeService metaExchangeService = new MetaExchangeService();
        private readonly ILogger<MetaExchangeController> _logger;
        private readonly IMemoryCache _memoryCache;

        public MetaExchangeController(ILogger<MetaExchangeController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        /** 
         * GET api/metaexchange/quote
         * This endpoint returns the best path to buy or sell a given amount of BTC
         * taking into account exchange balances and cross exchange swaps.
         * 
         * @param amount The amount of BTC to buy or sell.
         * @param type The type of the order (Buy or Sell).
         * @return The best paths/prices to buy or sell a given amount of BTC.
         **/
        [HttpGet("quote")]
        public async Task<ActionResult<string>> GetQuote(double amount, string type)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Cached on startup to avoid reading the file every time. Caching logic is implemented in OrderBookHostedService.
            List<SortedOrderEntry>? sortedOrders = (type == "Buy") ? _memoryCache.Get<List<SortedOrderEntry>>("sortedAsks") : _memoryCache.Get<List<SortedOrderEntry>>("sortedBids");
            Dictionary<string, OrderBookBalances>? exchangeBalances = _memoryCache.Get<Dictionary<string, OrderBookBalances>>("exchangeBalances");

            // This should never happen, but we need to handle it gracefully.
            if (sortedOrders == null || exchangeBalances == null)
            {
                return StatusCode(500, "Internal Server Error. No liquidity."); ;
            }

            // Match the order against the order books and find the best paths/prices.
            BestPathResponse? bestPath = await metaExchangeService.MatchOrdersAsync(amount, type, sortedOrders, exchangeBalances);

            // This should not happen, but we need to handle it gracefully.
            if (bestPath == null)
            {
                return BadRequest("No paths found. Try a lower amount.");
            }

            watch.Stop();
            _logger.LogDebug($"GetQuote | Execution Time: {watch.ElapsedMilliseconds} ms");

            return Ok(bestPath);
        }

        // This endpoint can be used to submit a trade order. It is not implemented in the current version of the API.
        [HttpPost("trade")]
        public string SubmitTrade(double amount, string type)
        {
            return $"Your trade to {type} {amount} BTC was successfully submitted";
        }
    }
}