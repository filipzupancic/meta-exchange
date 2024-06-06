public class OrderBook
{
    public DateTime AcqTime { get; set; }
    public List<OrderEntry> Bids { get; set; } = new List<OrderEntry>();  // Initialized to ensure it's never null
    public List<OrderEntry> Asks { get; set; } = new List<OrderEntry>();  // Initialized to ensure it's never null
}

public class OrderEntry
{
    public Order Order { get; set; } = new Order();  // Initialize to avoid null
}

public class Order
{
    public string? Id { get; set; }  // Nullable for optional identifier
    public DateTime Time { get; set; }
    public string? Type { get; set; }  // Nullable, assuming 'Type' could be optional
    public string? Kind { get; set; }  // Nullable to allow flexibility in defining order side
    public double Amount { get; set; }
    public double Price { get; set; }
}