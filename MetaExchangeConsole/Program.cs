namespace MetaExchangeConsole;

using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text;

class Program
{
    // We separate the logic for loading order books and matching orders into separate classes.
    private readonly OrderBookLoader orderBookLoader = new OrderBookLoader();
    private readonly OrderBookMatcher orderBookMatcher = new OrderBookMatcher();

    static void Main(string[] args)
    {
        var program = new Program();
        program.Run();
    }

    private void Run()
    {
        double tradeAmount = PromptForTradeAmount("Please enter the trade amount (a number):");
        string tradeType = PromptForTradeType("Please enter the trade type (e.g., Buy, Sell):");

        Console.WriteLine($"You entered a trade amount of: {tradeAmount} and a trade type of: {tradeType}");

        // Load order books and match orders sequentially
        //var orderBooks = orderBookLoader.LoadOrderBooks("../MetaExchangeApi/Data/order_books_data");
        //Dictionary<string, double> exchangeToBestPrice = orderBookMatcher.MatchOrders(tradeAmount, tradeType, orderBooks);

        // Load order books and match orders in parallel
        List<OrderBook> orderBooksParallel = orderBookLoader.LoadOrderBooksParallel("../MetaExchangeApi/Data/order_books_data");
        List<SortedOrderEntry> sortedOrders = orderBookMatcher.SortOrdersBySide(orderBooksParallel, tradeType);
        Dictionary<string, OrderBookBalances> exchangeBalances = orderBookMatcher.LoadBalances(orderBooksParallel);

        BestPathResponse? bestPath = orderBookMatcher.MatchOrders(tradeAmount, tradeType, sortedOrders, exchangeBalances);

        if (bestPath == null)
        {
            Console.WriteLine("No paths found. Try a lower amount.");
            return;
        }

        var result = new StringBuilder();

        result.AppendLine("Path found:");
        result.AppendLine($"Total Filled Amount: {Math.Round(bestPath.TotalAmount, 6)} BTC");
        result.AppendLine($"Total Price: {Math.Round(bestPath.AveragePrice * bestPath.TotalAmount, 6)} EUR");
        result.AppendLine($"Average Price: {Math.Round(bestPath.AveragePrice, 6)} EUR");


        foreach (var fill in bestPath.ExchangeDetails)
        {
            result.AppendLine($"Exchange: {fill.ExchangeId}, Filled Amount: {Math.Round(fill.FilledAmount, 6)} BTC, Average Price: {Math.Round(fill.AveragePrice, 6)} EUR, Remaining BTC: {Math.Round(fill.RemainingBalanceBtc, 6)}, Remaining EUR: {Math.Round(fill.RemainingBalanceEur, 6)}");
        }

        Console.WriteLine(result.ToString());
    }

    private double PromptForTradeAmount(string message)
    {
        Console.WriteLine(message);

        string input = Console.ReadLine()!;
        double tradeAmount;

        while (!double.TryParse(input, out tradeAmount))
        {
            Console.WriteLine("Invalid input for amount. Please enter a valid number:");
            input = Console.ReadLine()!;
        }

        return tradeAmount;
    }

    private string PromptForTradeType(string message)
    {
        Console.WriteLine(message);

        Console.WriteLine("Enter trade type:");

        string tradeType = Console.ReadLine()!.Trim();

        while (tradeType != "Buy" && tradeType != "Sell")
        {
            Console.WriteLine("Invalid input for trade type. Please enter either 'buy' or 'sell':");
            tradeType = Console.ReadLine()!.Trim();
        }

        return tradeType;
    }
}
