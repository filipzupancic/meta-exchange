/*
 * This model is used to represent the response of the best path calculation.
 *
 * @param TotalAmount The total amount of the best path.
 * @param AveragePrice The average price of the best path.
 */
public class BestPathResponse
{
    public double TotalAmount { get; set; }
    public double AveragePrice { get; set; }
    public double TotalPrice { get; set; }
    public List<ExchangePathDetail> ExchangeDetails { get; set; } = new List<ExchangePathDetail>();
}

/**
 * Represents the details of an exchange in the best path.
 *
 * @param ExchangeId The unique identifier of the exchange.
 * @param FilledAmount The amount filled in the exchange.
 * @param AveragePrice The average price of filled amount the exchange.
 * @param RemainingBalanceBtc The remaining balance in BTC.
 * @param RemainingBalanceEur The remaining balance in EUR.
 */
public class ExchangePathDetail
{
    public required string ExchangeId { get; set; }
    public double FilledAmount { get; set; }
    public double AveragePrice { get; set; }
    public double RemainingBalanceBtc { get; set; }
    public double RemainingBalanceEur { get; set; }
}
