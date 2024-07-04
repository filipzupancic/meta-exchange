namespace MetaExchangeTest;

using Humanizer;
using MetaExchangeApi.Models;
using MetaExchangeApi.Services;

[TestClass]
public class OrderBookMatchingTest
{
    private static readonly MetaExchangeService _metaExchangeService = new MetaExchangeService();

    private static readonly string id1 = "1548759600.25189";
    private static readonly string id2 = "1548759601.33694";
    private static DateTime acqTime = DateTime.Now;
    private static OrderBook orderBook1 = new OrderBook
    {
        Id = id1,
        AcqTime = acqTime,
        Asks = new List<OrderEntry>
            {
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 0.2,
                        Price = 3000.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "2",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 0.62,
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
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 3,
                        Price = 2900.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "2",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 0.1,
                        Price = 2870.0
                    }
                }
            }
    };

    private static OrderBook orderBook2 = new OrderBook
    {
        Id = id2,
        AcqTime = acqTime,
        Asks = new List<OrderEntry>
            {
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "1",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 0.7,
                        Price = 3100.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "2",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Sell",
                        Amount = 1.2,
                        Price = 3200.0
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
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 0.8,
                        Price = 2880.0
                    }
                },
                new OrderEntry
                {
                    Order = new Order
                    {
                        Id = "2",
                        Time = acqTime,
                        Type = "Limit",
                        Kind = "Buy",
                        Amount = 1.5,
                        Price = 2820.0
                    }
                }
            }
    };

    private static List<OrderBook> orderBooks = new List<OrderBook> { orderBook1, orderBook2 };


    [TestMethod]
    public async Task TestOrderBookMatchingBuyNoBalanceLimit()
    {
        double amount = 2.0;
        string type = "Buy";

        Dictionary<string, OrderBookBalances> exchangeBalances = new Dictionary<string, OrderBookBalances>
        {
            { id1, new OrderBookBalances { BalanceBtc = 100, BalanceEur = 30000.0 } },
            { id2, new OrderBookBalances { BalanceBtc = 100, BalanceEur = 60000.0 } }
        };

        List<SortedOrderEntry> sortedAsks = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Buy");
        BestPathResponse? bestPrices = await _metaExchangeService.MatchOrdersAsync(amount, type, sortedAsks, exchangeBalances);

        Assert.AreEqual(bestPrices!.TotalAmount, 2.0);
        Assert.AreEqual(bestPrices.AveragePrice, 3145.0);
        Assert.AreEqual(bestPrices.ExchangeDetails.Count, 2);

        Assert.AreEqual(bestPrices.ExchangeDetails[0].AveragePrice, 3000.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].FilledAmount, 0.2);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceBtc, 100);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceEur, 29400.0);

        Assert.AreEqual(bestPrices.ExchangeDetails[1].AveragePrice, 3161.111111);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].FilledAmount, 1.8);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceBtc, 100);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceEur, 54310.0);
    }

    [TestMethod]
    public async Task TestOrderBookMatchingBuyBalanceLimit()
    {
        double amount = 2.0;
        string type = "Buy";

        Dictionary<string, OrderBookBalances> exchangeBalances = new Dictionary<string, OrderBookBalances>
        {
            { id1, new OrderBookBalances { BalanceBtc = 0.5, BalanceEur = 3000.0 } },
            { id2, new OrderBookBalances { BalanceBtc = 10, BalanceEur = 4000.0 } }
        };

        List<SortedOrderEntry> sortedAsks = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Buy");
        BestPathResponse? bestPrices = await _metaExchangeService.MatchOrdersAsync(amount, type, sortedAsks, exchangeBalances);

        Assert.AreEqual(bestPrices!.TotalAmount, 2.0);
        Assert.AreEqual(bestPrices.AveragePrice, 3171.40625);
        Assert.AreEqual(bestPrices.ExchangeDetails.Count, 2);

        Assert.AreEqual(bestPrices.ExchangeDetails[0].AveragePrice, 3217.596567);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].FilledAmount, 0.728125);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceBtc, 0.5);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceEur, 657.1875);

        Assert.AreEqual(bestPrices.ExchangeDetails[1].AveragePrice, 3144.963145);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].FilledAmount, 1.271875);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceBtc, 10.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceEur, 0.0);
    }

    [TestMethod]
    public async Task TestOrderBookMatchingSellNoBalanceLimit()
    {
        double amount = 2.0;
        string type = "Sell";

        Dictionary<string, OrderBookBalances> exchangeBalances = new Dictionary<string, OrderBookBalances>
        {
            { id1, new OrderBookBalances { BalanceBtc = 100, BalanceEur = 30000.0 } },
            { id2, new OrderBookBalances { BalanceBtc = 100, BalanceEur = 60000.0 } }
        };

        List<SortedOrderEntry> sortedBids = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Sell");
        BestPathResponse? bestPrices = await _metaExchangeService.MatchOrdersAsync(amount, type, sortedBids, exchangeBalances);

        Assert.AreEqual(bestPrices!.TotalAmount, 2.0);
        Assert.AreEqual(bestPrices.AveragePrice, 2900.0);
        Assert.AreEqual(bestPrices.ExchangeDetails.Count, 1);

        Assert.AreEqual(bestPrices.ExchangeDetails[0].AveragePrice, 2900.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].FilledAmount, 2.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceBtc, 98.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceEur, 30000.0);
    }

    [TestMethod]
    public async Task TestOrderBookMatchingSellBalanceLimit()
    {
        double amount = 2.0;
        string type = "Sell";

        Dictionary<string, OrderBookBalances> exchangeBalances = new Dictionary<string, OrderBookBalances>
        {
            { id1, new OrderBookBalances { BalanceBtc = 1, BalanceEur = 3000.0 } },
            { id2, new OrderBookBalances { BalanceBtc = 2, BalanceEur = 6000.0 } }
        };

        List<SortedOrderEntry> sortedBids = await _metaExchangeService.SortOrdersBySideAsync(orderBooks, "Sell");
        BestPathResponse? bestPrices = await _metaExchangeService.MatchOrdersAsync(amount, type, sortedBids, exchangeBalances);

        Assert.AreEqual(bestPrices!.TotalAmount, 2.0);
        Assert.AreEqual(bestPrices.AveragePrice, 2884.0);
        Assert.AreEqual(bestPrices.ExchangeDetails.Count, 2);

        Assert.AreEqual(bestPrices.ExchangeDetails[0].AveragePrice, 2900.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].FilledAmount, 1.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceBtc, 0.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[0].RemainingBalanceEur, 3000.0);

        Assert.AreEqual(bestPrices.ExchangeDetails[1].AveragePrice, 2868.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].FilledAmount, 1.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceBtc, 1.0);
        Assert.AreEqual(bestPrices.ExchangeDetails[1].RemainingBalanceEur, 6000.0);
    }
}

