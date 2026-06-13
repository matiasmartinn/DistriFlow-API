namespace DistriFlow.Application.PriceLists.DTOs.Responses;

public sealed record PriceListDto(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<ProductPriceDto> ProductPrices);
