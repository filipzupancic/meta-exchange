namespace MetaExchangeApi.Services;

using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using MetaExchangeApi.Models;
using System.Collections.Concurrent;

public class MetaExchangeService
{
    /**
    * Load order books from a file serial. This is not used in the current implementation but
    * I wanted to keep it here for reference. This approach is slower compared to LoadOrderBooksParallel
    * since it processes lines one by one. In the case of this exercise we are loading small order books
    * and on average it takes 1 second to load all order books sequentially.
    * 
    * @param filePath The path to the file containing the order books.
    * @return A list of order books.
    **/
    public async Task<List<OrderBook>> LoadOrderBooks(string filePath)
    {
        var orderBooks = new List<OrderBook>();
        var lines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in lines)
        {
            var jsonPart = line.Split('\t')[1];
            var orderBook = JsonSerializer.Deserialize<OrderBook>(jsonPart);

            if (orderBook != null)
            {
                orderBooks.Add(orderBook);
            }
        }

        return orderBooks;
    }


    /**
    * Load order books from a file in parallel. This approach improves performance by around 50% 
    * compared to serial LoadOrderBooks. Since we are caching the order books this is only 
    * called once when. In the case of this exercise we are loading small order books directly from 
    * file in reality we would probably store order books to a database and use more complex logic
    * for loading and caching.
    *
    * @param filePath The path to the file containing the order books.
    * @return A list of order books.
    **/
    public async Task<List<OrderBook>> LoadOrderBooksParallel(string filePath)
    {
        var orderBooks = new ConcurrentBag<OrderBook>();
        var lines = await File.ReadAllLinesAsync(filePath);

        // Use Parallel.ForEach to process lines in parallel
        Parallel.ForEach(lines, (line) =>
        {
            var jsonPart = line.Split('\t')[1];
            var orderBook = JsonSerializer.Deserialize<OrderBook>(jsonPart);

            if (orderBook != null)
            {
                orderBooks.Add(orderBook);
            }
        });

        // Convert ConcurrentBag to List before returning
        return new List<OrderBook>(orderBooks);
    }

    /**
    * Match a trade order against the order books and find paths with best price.
    *
    * Potential improvements: we would probably want to separate the matching logic from the
    * I/O operations to a different service like OrderBookMatchingService. We could also consider 
    * using a different data structures for loading / matching. Additionally we could execute matching
    * in parallel for each order book. In this case I am satisfied with the performance of the current
    * implementation and I don't see a need for further optimizations since the order books are small and
    * average matching execution time is 1 ms.
    *
    * Additional note: 
    * In the task description it was stated that each line represents a different exchange.
    * It was also stated that there are BTC and EUR balances on each exchange. I wasn't sure where to find the
    * balances so I didn't use them in the matching logic. We could take as balances the first number in each line
    * that is separated by the dot (1548759600.25189) and is actually the time of the order book snapshot but in
    * reality the order book depth on each exchange is lower than those balances so It wouldn't make any difference
    * If we had the balances we would simply add a constraint to the matching logic to check if
    * there is enough balance on the exchange to execute the trade. I can defend this decision if needed or show how
    * I would implement it in the office. :D
    * 
    * @param trade The trade order to match.
    * @param orderBooks The list of order books to match against.
    *
    * @return The best price we can get given the amount and type.
    **/
    public async Task<Dictionary<string, double>> MatchOrderAsync(double tradeAmount, string tradeType, List<OrderBook> orderBooks)
    {
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

        await Task.CompletedTask;

        return result;
    }
}