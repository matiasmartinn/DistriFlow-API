using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.DistributorConfigurations.Enums;
using DistriFlow.Domain.Orders.ValueObjects;

namespace DistriFlow.Domain.Orders.Services;

public static class OrderLinePricing
{
    public static OrderLinePrice Resolve(
        PriceList? effectivePriceList,
        PriceFallbackPolicy fallbackPolicy,
        Product product
    )
    {
        var listPrice = effectivePriceList?.FindPrice(product.Id);
        if (listPrice.HasValue)
            return OrderLinePrice.FromPriceList(
                listPrice.Value,
                effectivePriceList!.Id,
                effectivePriceList.Version);

        return fallbackPolicy == PriceFallbackPolicy.UseLastValidPrice
            ? OrderLinePrice.FromLastValidPrice(product.BasePrice)
            : OrderLinePrice.Unavailable;
    }
}