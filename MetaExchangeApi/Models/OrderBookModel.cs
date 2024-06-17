namespace MetaExchangeApi.Models;

/**
 * Represents an order book.
 *
 * @param Id The unique identifier of the order book.
 * @param AcqTime The time when the order book was acquired.
 * @param Bids The list of buy orders.
 * @param Asks The list of sell orders.
 **/
public class OrderBook
{
    public string? Id { get; set; }
    public DateTime AcqTime { get; set; }
    public required List<OrderEntry> Bids { get; set; }
    public required List<OrderEntry> Asks { get; set; }
}

/**
 * Represents an order entry. This was added so we can parse 
 * the order book data from file order_books_data.
 *
 * @param Order The order.
 **/
public class OrderEntry
{
    public required Order Order { get; set; }
}

/**
 * Represents an order.
 *
 * @param Id The unique identifier of the order.
 * @param Time The time when the order was placed.
 * @param Side The side of the order (Buy or Sell).
 * @param Type The type of the order (Limit or Market).
 * @param Amount The amount of the order.
 * @param Price The price of the order.
 **/
public class Order
{
    public string? Id { get; set; }
    public DateTime Time { get; set; }
    public string? Type { get; set; }
    public string? Kind { get; set; }
    public double Amount { get; set; }
    public double Price { get; set; }
}

/**
 * Represents a sorted order. We'll use it to sort the orders by price 
 * and amount depending on trade side.
 *
 * @param OrderBookId The unique identifier of the order book.
 * @param Amount The amount of the order.
 * @param Price The price of the order.
 **/
public class SortedOrderEntry
{
    public required string OrderBookId { get; set; }
    public double Amount { get; set; }
    public double Price { get; set; }
}

/**
 * Represents the balances of an order book / exchange.
 *
 * @param BalanceBtc The balance in BTC.
 * @param BalanceEur The balance in EUR.
 **/
public class OrderBookBalances
{
    public double BalanceBtc { get; set; }
    public double BalanceEur { get; set; }
}
