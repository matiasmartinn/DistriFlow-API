namespace DistriFlow.Application.Stocks.DTOs.Requests;

public sealed record IncreaseStockRequest
{
    public decimal Amount { get; init; }
}
