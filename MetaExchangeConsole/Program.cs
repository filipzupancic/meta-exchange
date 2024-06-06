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
        var orderBooksParallel = orderBookLoader.LoadOrderBooksParallel("../MetaExchangeApi/Data/order_books_data");
        Dictionary<string, double> exchangeToBestPrice = orderBookMatcher.MatchOrders(tradeAmount, tradeType, orderBooksParallel);

        if (exchangeToBestPrice.Count == 0)
        {
            Console.WriteLine("No paths found try lower amount.");
        }
        else
        {
            // Initialize a StringBuilder to construct the result string
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Best prices for Trade Order {tradeType} {tradeAmount} BTC:");

            // Iterate through each exchange with best price and append to the result string
            // Exchange is represented by timestamp as unique identifier as this is the unique
            // identifier for each line in the order books data. It's a bit weird but it works.
            // In the real production app we would probably use exchange name or id but I took
            // this approach to avoid unnecessary complexity because data is already in this format.
            foreach (var (exchange, bestPrice) in exchangeToBestPrice)
            {
                result.AppendLine($"Best price at exchange {exchange} is {bestPrice} EUR");
            }

            Console.WriteLine(result.ToString());
        }
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
