using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.PriceLists.ValueObjects;

namespace DistriFlow.Domain.Orders.ValueObjects;

public sealed record OrderLinePrice
{
    public OrderLinePriceStatus Status { get; init; }
    public Money SourceUnitPrice { get; init; }
    public Money AppliedUnitPrice { get; init; }
    public PriceOrigin PriceOrigin { get; private init; }
    public PriceListId? PriceListId { get; private init; }
    public int? PriceListVersion { get; private init; }

    public static OrderLinePrice FromPriceList(
        Money unitPrice, PriceListId priceListId, int version) => new()
        {
            Status = OrderLinePriceStatus.Resolved,
            SourceUnitPrice = unitPrice,
            AppliedUnitPrice = unitPrice,
            PriceOrigin = PriceOrigin.PriceList,
            PriceListId = priceListId,
            PriceListVersion = version
        };

    public static OrderLinePrice FromLastValidPrice(Money basePrice) => new()
    {
        Status = OrderLinePriceStatus.Resolved,
        SourceUnitPrice = basePrice,
        AppliedUnitPrice = basePrice,
        PriceOrigin = PriceOrigin.LastValidPrice
    };

    public static readonly OrderLinePrice Unavailable = new()
    {
        Status = OrderLinePriceStatus.Unavailable
    };
}