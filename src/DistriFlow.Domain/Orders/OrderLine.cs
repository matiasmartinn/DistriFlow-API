using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.Orders;

public sealed class OrderLine
{
    public ProductId ProductId { get; private set; }
    public string ProductDescription { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public PriceListId? PriceListId { get; private set; }
    public int? PriceListVersion { get; private set; }
    public Money SourceUnitPrice { get; private set; }
    public Money AppliedUnitPrice { get; private set; }
    public PriceOrigin PriceOrigin { get; private set; }
    public Money UnitPrice => AppliedUnitPrice;
    public Money Subtotal => Money.Create(Quantity.Amount * AppliedUnitPrice.Amount);

    private OrderLine() { }

    private OrderLine(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money sourceUnitPrice,
        Money appliedUnitPrice,
        PriceOrigin priceOrigin,
        PriceListId? priceListId,
        int? priceListVersion)
    {
        ProductId = productId;
        ProductDescription = productDescription;
        Quantity = quantity;
        SourceUnitPrice = sourceUnitPrice;
        AppliedUnitPrice = appliedUnitPrice;
        PriceOrigin = priceOrigin;
        PriceListId = priceListId;
        PriceListVersion = priceListVersion;
    }

    internal static OrderLine Create(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money sourceUnitPrice,
        Money appliedUnitPrice,
        PriceOrigin priceOrigin,
        PriceListId? priceListId,
        int? priceListVersion)
    {
        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto de la linea es requerido.");

        if (string.IsNullOrWhiteSpace(productDescription))
            throw new DomainException("La descripcion del producto es requerida.");

        EnsurePositive(quantity);
        EnsurePriceTraceability(priceOrigin, priceListId, priceListVersion);

        return new OrderLine(
            productId,
            productDescription.Trim(),
            quantity,
            sourceUnitPrice,
            appliedUnitPrice,
            priceOrigin,
            priceListId,
            priceListVersion);
    }

    internal void ChangeQuantity(Quantity quantity)
    {
        EnsurePositive(quantity);

        if (!Quantity.HasSameUnitAs(quantity))
        {
            throw new DomainException(
                "La unidad de la cantidad no coincide con la unidad de la linea.");
        }

        Quantity = quantity;
    }

    private static void EnsurePositive(Quantity quantity)
    {
        if (quantity.Amount == 0)
            throw new DomainException("La cantidad de la linea debe ser mayor a cero.");
    }

    private static void EnsurePriceTraceability(
        PriceOrigin priceOrigin,
        PriceListId? priceListId,
        int? priceListVersion)
    {
        if (!Enum.IsDefined(priceOrigin))
            throw new DomainException("El origen del precio es invalido.");

        if (priceOrigin == PriceOrigin.PriceList)
        {
            if (!priceListId.HasValue || priceListVersion is null or <= 0)
                throw new DomainException(
                    "La lista y version del precio son requeridas.");

            return;
        }

        if (priceListId.HasValue || priceListVersion.HasValue)
            throw new DomainException(
                "El precio de respaldo no debe referenciar una lista de precios.");
    }
}
