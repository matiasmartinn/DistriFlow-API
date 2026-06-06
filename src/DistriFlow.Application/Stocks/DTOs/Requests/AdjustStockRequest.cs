namespace DistriFlow.Application.Stocks.DTOs.Requests;

public sealed record AdjustStockRequest
{
    public decimal Amount { get; init; }
}
