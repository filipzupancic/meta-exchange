namespace MetaExchangeConsole;

public class OrderBookMatcher
{
    public Dictionary<string, double> MatchOrders(double tradeAmount, string tradeType, List<OrderBook> orderBooks)
    {
        // Measure execution time in milliseconds
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        // Dictionary to store the best price for each exchange.
        // Exchange is represented by the time when the order book was acquired.
        var exchangeToPrice = new Dictionary<string, double>();
        var bestPrice = -1.0;
        foreach (var orderBook in orderBooks)
        {
            var orders = (tradeType == "Buy") ? orderBook.Asks : orderBook.Bids;
            if (orders == null || orders.Count == 0)
            {
                continue;
            }

            var remainingTradeAmount = tradeAmount;
            var currentPrice = 0.0;
            foreach (var order in orders)
            {
                double orderPrice = order.Order.Price;
                double orderAmount = order.Order.Amount;

                if (orderAmount >= remainingTradeAmount)
                {
                    currentPrice += remainingTradeAmount * orderPrice;
                    remainingTradeAmount = 0;
                    break;
                }
                else
                {
                    currentPrice += orderAmount * orderPrice;
                    remainingTradeAmount -= orderAmount;
                }
            }

            if (remainingTradeAmount == 0)
            {
                exchangeToPrice[orderBook.AcqTime.ToString()] = currentPrice;
                if (bestPrice == -1 || currentPrice < bestPrice)
                {
                    bestPrice = currentPrice;
                }
            }
        }

        // Filter the dictionary to return only entries that match the bestPrice
        var result = (bestPrice == -1)
            ? new Dictionary<string, double>()
            : exchangeToPrice.Where(p => p.Value == bestPrice).ToDictionary(p => p.Key, p => p.Value);

        watch.Stop();
        Console.WriteLine($"MatchOrders | Execution Time: {watch.ElapsedMilliseconds} ms");

        return result;
    }
}