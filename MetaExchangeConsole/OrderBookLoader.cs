namespace MetaExchangeConsole;

using System.Collections.Concurrent;
using System.Text.Json;

public class OrderBookLoader
{
    public List<OrderBook> LoadOrderBooks(string filePath)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var orderBooks = new List<OrderBook>();
        var lines = File.ReadAllLines(filePath); // Synchronous read

        foreach (var line in lines)
        {
            var jsonPart = line.Split('\t')[1];
            var orderBook = JsonSerializer.Deserialize<OrderBook>(jsonPart); // Assuming synchronous deserialization

            if (orderBook != null)
            {
                orderBooks.Add(orderBook);
            }
        }

        watch.Stop();
        Console.WriteLine($"LoadOrderBooks sequentially | Execution Time: {watch.ElapsedMilliseconds} ms");

        return orderBooks;
    }

    public List<OrderBook> LoadOrderBooksParallel(string filePath)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var orderBooks = new ConcurrentBag<OrderBook>();
        var lines = File.ReadAllLines(filePath); // Synchronous read

        // Use Parallel.ForEach to process lines in parallel
        Parallel.ForEach(lines, (line) =>
        {
            var jsonPart = line.Split('\t')[1];
            var orderBook = JsonSerializer.Deserialize<OrderBook>(jsonPart); // Assuming synchronous deserialization

            if (orderBook != null)
            {
                orderBooks.Add(orderBook);
            }
        });

        watch.Stop();
        Console.WriteLine($"LoadOrderBooksParallel | Execution Time: {watch.ElapsedMilliseconds} ms");

        return new List<OrderBook>(orderBooks);
    }
}