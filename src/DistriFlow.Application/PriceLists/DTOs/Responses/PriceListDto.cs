namespace DistriFlow.Application.PriceLists.DTOs.Responses;

public sealed record PriceListDto(
    Guid Id,
    string Name,
    IReadOnlyList<ProductPriceDto> ProductPrices);
