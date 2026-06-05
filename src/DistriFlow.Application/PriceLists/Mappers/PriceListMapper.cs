using DistriFlow.Application.PriceLists.DTOs.Responses;
using DistriFlow.Domain.PriceLists;

namespace DistriFlow.Application.PriceLists.Mappers;

internal static class PriceListMapper
{
    internal static PriceListDto ToDto(this PriceList priceList)
    {
        return new PriceListDto(
            priceList.Id.Value,
            priceList.Name,
            priceList.ProductPrices
                .Select(productPrice => new ProductPriceDto(
                    productPrice.ProductId.Value,
                    productPrice.Price.Amount))
                .ToList());
    }
}
