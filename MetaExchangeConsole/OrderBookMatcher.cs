namespace MetaExchangeConsole;

using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class OrderBookMatcher
{
    private readonly Random random = new Random();

    public List<SortedOrderEntry> SortOrdersBySide(List<OrderBook> orderBooks, string side)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

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

        watch.Stop();
        Console.WriteLine($"SortOrdersBySide | Execution Time: {watch.ElapsedMilliseconds} ms");

        return sortedResult;
    }

    public Dictionary<string, OrderBookBalances> LoadBalances(List<OrderBook> orderBooks)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var exchangeBalances = new ConcurrentDictionary<string, OrderBookBalances>();

        Parallel.ForEach(orderBooks, orderBook =>
        {
            var balance = new OrderBookBalances
            {
                BalanceBtc = random.NextDouble() * 500,
                BalanceEur = random.NextDouble() * 500 * 3000
            };

            exchangeBalances[orderBook.Id!] = balance;
        });

        watch.Stop();
        Console.WriteLine($"LoadBalances | Execution Time: {watch.ElapsedMilliseconds} ms");

        return new Dictionary<string, OrderBookBalances>(exchangeBalances);
    }

    public BestPathResponse? MatchOrders(double amountIn, string side, List<SortedOrderEntry> availableOrders, Dictionary<string, OrderBookBalances> exchangeBalances)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

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

        watch.Stop();
        Console.WriteLine($"MatchOrders | Execution Time: {watch.ElapsedMilliseconds} ms");

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
    }

}