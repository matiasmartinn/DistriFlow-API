namespace DistriFlow.Application.Stocks.DTOs.Responses;

public sealed record StockDto(
    Guid ProductId,
    decimal Available,
    string SaleUnit);
