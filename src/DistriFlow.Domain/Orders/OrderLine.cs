using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.Orders;

public sealed class OrderLine
{
    public ProductId ProductId { get; private set; }
    public string ProductDescription { get; private set; } = null!;
    public Quantity Quantity { get; private set; } = null!;
    public Money UnitPrice { get; private set; }
    public Money Subtotal => Money.Create(Quantity.Amount * UnitPrice.Amount);

    private OrderLine() { }

    private OrderLine(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money unitPrice)
    {
        ProductId = productId;
        ProductDescription = productDescription;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal static OrderLine Create(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money unitPrice)
    {
        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto de la línea es requerido.");

        if (string.IsNullOrWhiteSpace(productDescription))
            throw new DomainException("La descripción del producto es requerida.");

        EnsurePositive(quantity);

        return new OrderLine(
            productId,
            productDescription.Trim(),
            quantity,
            unitPrice);
    }

    internal void ChangeQuantity(Quantity quantity)
    {
        EnsurePositive(quantity);

        if (!Quantity.HasSameUnitAs(quantity))
        {
            throw new DomainException(
                "La unidad de la cantidad no coincide con la unidad de la línea.");
        }

        Quantity = quantity;
    }

    private static void EnsurePositive(Quantity quantity)
    {
        if (quantity.Amount == 0)
            throw new DomainException("La cantidad de la línea debe ser mayor a cero.");
    }
}
