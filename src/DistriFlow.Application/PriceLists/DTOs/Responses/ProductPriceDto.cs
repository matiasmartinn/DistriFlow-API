namespace DistriFlow.Application.PriceLists.DTOs.Responses;

public sealed record ProductPriceDto(
    Guid ProductId,
    decimal Price);
