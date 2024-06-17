namespace MetaExchangeConsole;

using System.Collections.Concurrent;
using System.Text.Json;

public class OrderBookLoader
{
    public List<OrderBook> LoadOrderBooksParallel(string filePath)
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var orderBooks = new ConcurrentBag<OrderBook>();
        var lines = File.ReadAllLines(filePath); // Synchronous read

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

        watch.Stop();
        Console.WriteLine($"LoadOrderBooksParallel | Execution Time: {watch.ElapsedMilliseconds} ms");

        return new List<OrderBook>(orderBooks);
    }
}