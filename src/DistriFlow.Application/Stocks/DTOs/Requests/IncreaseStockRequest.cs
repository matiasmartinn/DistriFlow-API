namespace DistriFlow.Application.Stocks.DTOs.Requests;

public sealed record IncreaseStockRequest
{
    public decimal Amount { get; init; }

    public bool IsInitialLoad { get; init; }

    public string? Reason { get; init; }
}
