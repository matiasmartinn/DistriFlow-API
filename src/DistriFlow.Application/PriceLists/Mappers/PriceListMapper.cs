using DistriFlow.Application.PriceLists.DTOs.Responses;
using DistriFlow.Domain.PriceLists;

namespace DistriFlow.Application.PriceLists.Mappers;

internal static class PriceListMapper
{
    internal static PriceListDto ToDto(this PriceList priceList)
    {
        return new PriceListDto(
            priceList.Id.Value,
            priceList.SeriesId.Value,
            priceList.Name,
            priceList.Status.ToString(),
            priceList.Version,
            priceList.ValidFromUtc,
            priceList.ValidUntilUtc,
            priceList.ActivatedAtUtc,
            priceList.DeactivatedAtUtc,
            priceList.CreatedByUserId.Value,
            priceList.UpdatedByUserId.Value,
            priceList.RowVersion,
            priceList.ProductPrices
                .Select(productPrice => new ProductPriceDto(
                    productPrice.ProductId.Value,
                    productPrice.Price.Amount))
                .ToList());
    }
}
