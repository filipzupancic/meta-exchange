namespace MetaExchangeApi.Services;

using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using MetaExchangeApi.Models;
using System.Collections.Concurrent;

public class MetaExchangeService
{
    private readonly Random random = new Random();

    /**
    * Load order books from a file in parallel. This approach improves performance by around 50% 
    * compared to serial LoadOrderBooks.
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
            string[] jsonParts = line.Split('\t');
            string timestamp = jsonParts[0];
            string json = jsonParts[1];
            var orderBook = JsonSerializer.Deserialize<OrderBook>(json);

            orderBook!.Id = timestamp;

            if (orderBook != null)
            {
                orderBooks.Add(orderBook);
            }
        });

        // Convert ConcurrentBag to List before returning
        return new List<OrderBook>(orderBooks);
    }

    /**
        * Sort orders by side (Buy or Sell) and return a list of sorted orders. We'll cache the sorted orders
        * in memory to avoid sorting them every time we need to match a trade order. This significantly improves
        * performance.
        *
        * @param orderBooks The list of order books to sort.
        * @param side The side of the orders to sort (Buy or Sell).
        * @return A list of sorted orders either Bids in case of Sell or Asks in case of Buy.
        **/
    public async Task<List<SortedOrderEntry>> SortOrdersBySideAsync(List<OrderBook> orderBooks, string side)
    {
        return await Task.Run(() =>
        {
            var result = new ConcurrentBag<SortedOrderEntry>();

            // Use Parallel.ForEach to process orderBooks in parallel
            Parallel.ForEach(orderBooks, orderBook =>
            {
                var orders = (side == "Sell") ? orderBook.Bids : orderBook.Asks;
                foreach (var order in orders)
                {
                    result.Add(new SortedOrderEntry
                    {
                        OrderBookId = orderBook.Id!,
                        Amount = order.Order.Amount,
                        Price = order.Order.Price
                    });
                }
            });

            var sortedResult = (side == "Sell")
                ? result.OrderByDescending(o => o.Price).ThenByDescending(o => o.Amount).ToList()
                : result.OrderBy(o => o.Price).ThenByDescending(o => o.Amount).ToList();

            return sortedResult;
        });
    }

    /**
    * Load balances for each exchange from the order books. We'll use the balances to calculate the best path
    * for a trade order. We'll cache the balances in memory to avoid loading them every time we need to match a trade order.
    * Balances are randomly generated for each exchange and the amount is chosen empirically based on order book liquidity/depth.
    *
    * @param orderBooks The list of order books to load balances from.
    * @return A dictionary of exchange balances.
    **/
    public async Task<Dictionary<string, OrderBookBalances>> LoadBalancesAsync(List<OrderBook> orderBooks)
    {
        return await Task.Run(() =>
        {
            var exchangeBalances = new ConcurrentDictionary<string, OrderBookBalances>();

            Parallel.ForEach(orderBooks, orderBook =>
            {
                var balance = new OrderBookBalances
                {
                    BalanceBtc = random.NextDouble() * 5,
                    BalanceEur = random.NextDouble() * 5 * 3000
                };

                exchangeBalances[orderBook.Id!] = balance;
            });

            return new Dictionary<string, OrderBookBalances>(exchangeBalances);
        });
    }

    /**
    * Match a trade order against the order books and find paths with best price. We match the order
    * against the available orders in the order books. Available orders are sorted first by price and amount
    * depending on the side of the trade order. We then iterate through the available orders and match the
    * trade order against them. We take into account the remaining balances on each of the exchanges we swap through.
    * 
    * @param amountIn The amount to trade.
    * @param side The side of the trade order (Buy or Sell).
    * @param availableOrders The list of available orders to match against.
    * @param exchangeBalances The balances of the exchanges/order books.
    * @param orderBooks The list of order books to match against.
    *
    * @return The best price we can get given the amount and type. We return sum matched amount, average price and exchange details
    * which include the exchange id, filled amount and average price for each exchange we swap through.
    **/
    public async Task<BestPathResponse?> MatchOrdersAsync(double amountIn, string side, List<SortedOrderEntry> availableOrders, Dictionary<string, OrderBookBalances> exchangeBalances)
    {
        return await Task.Run(() =>
        {
            var remainingBalances = new Dictionary<string, OrderBookBalances>(exchangeBalances);
            var remainingAmount = amountIn;
            var totalCost = 0.0;
            var totalAmount = 0.0;

            var pathDetails = new Dictionary<string, ExchangePathDetail>();

            foreach (var order in availableOrders)
            {
                string exchangeId = order.OrderBookId;
                if (remainingAmount <= 0)
                    break;

                if (!remainingBalances.TryGetValue(exchangeId, out var balances))
                    continue;

                double maxAvailableAmount;
                if (side == "Buy")
                {
                    maxAvailableAmount = balances.BalanceEur / order.Price;
                }
                else // "Sell"
                {
                    maxAvailableAmount = balances.BalanceBtc;
                }

                double amountToTake = Math.Min(order.Amount, Math.Min(remainingAmount, maxAvailableAmount));

                if (amountToTake <= 0)
                    continue;

                remainingAmount -= amountToTake;
                totalCost += amountToTake * order.Price;
                totalAmount += amountToTake;

                if (side == "Buy")
                {
                    balances.BalanceEur -= amountToTake * order.Price;
                }
                else // "Sell"
                {
                    balances.BalanceBtc -= amountToTake;
                }

                if (!pathDetails.ContainsKey(exchangeId))
                {
                    pathDetails[exchangeId] = new ExchangePathDetail
                    {
                        ExchangeId = exchangeId,
                        FilledAmount = 0,
                        AveragePrice = 0
                    };
                }

                var exchangeDetail = pathDetails[exchangeId];
                exchangeDetail.FilledAmount += amountToTake;
                exchangeDetail.AveragePrice = ((exchangeDetail.AveragePrice * (exchangeDetail.FilledAmount - amountToTake)) + (amountToTake * order.Price)) / exchangeDetail.FilledAmount;
                exchangeDetail.RemainingBalanceBtc = balances.BalanceBtc;
                exchangeDetail.RemainingBalanceEur = balances.BalanceEur;
            }

            if (totalAmount > 0)
            {
                return new BestPathResponse
                {
                    TotalAmount = totalAmount,
                    AveragePrice = totalCost / totalAmount,
                    ExchangeDetails = pathDetails.Values.ToList()
                };
            }

            return null;
        });
    }
}
