namespace MetaExchangeTest;

using MetaExchangeApi.Models;
using MetaExchangeApi.Services;

[TestClass]
public class OrderBookMatchingTest
{
    private static MetaExchangeService _orderBookService = new MetaExchangeService();

    private static DateTime acqTime = DateTime.Now;
    private static OrderBook orderBook = new OrderBook
    {
        AcqTime = acqTime,
        Asks = new List<OrderEntry>
            {
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = DateTime.Now,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 7,
                        Price = 3000.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = DateTime.Now,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 4,
                        Price = 3300.0
                    }
                }
            },
        Bids = new List<OrderEntry>
            {
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = DateTime.Now,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 7,
                        Price = 2900.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = DateTime.Now,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 4,
                        Price = 2870.0
                    }
                }
            }
    };

    private static List<OrderBook> orderBooks = new List<OrderBook> { orderBook };

    [TestMethod]
    public async Task TestOrderBookMatching1()
    {
        double amount = 9.0;
        string type = "Buy";

        Dictionary<string, double> bestPrices = await _orderBookService.MatchOrderAsync(amount, type, orderBooks);

        Assert.AreEqual(bestPrices[acqTime.ToString()], 27600.0);
    }

    [TestMethod]
    public async Task TestOrderBookMatching2()
    {
        double amount = 9.0;
        string type = "Sell";

        Dictionary<string, double> bestPrices = await _orderBookService.MatchOrderAsync(amount, type, orderBooks);

        Assert.AreEqual(bestPrices[acqTime.ToString()], 26040.0);
    }
}

