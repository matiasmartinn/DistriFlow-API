namespace DistriFlow.Application.Products.DTOs.Responses;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Sku,
    string SaleUnit,
    decimal BasePrice,
    string Status);
