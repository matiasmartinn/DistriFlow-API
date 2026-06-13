namespace DistriFlow.Application.Products.DTOs.Responses;

public sealed record ProductWithStockDto(
    Guid Id,
    string Name,
    string? Sku,
    string SaleUnit,
    decimal BasePrice,
    string Status,
    decimal Available,
    long StockRowVersion);
