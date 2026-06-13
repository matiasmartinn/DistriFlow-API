using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.Stocks;

public sealed class Stock
{
    public StockId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public ProductId ProductId { get; private set; }
    public Quantity Available { get; private set; } = null!;
    public long RowVersion { get; private set; }

    private Stock() { }

    private Stock(
        StockId id,
        DistributorId distributorId,
        ProductId productId,
        Quantity available,
        long rowVersion)
    {
        Id = id;
        DistributorId = distributorId;
        ProductId = productId;
        Available = available;
        RowVersion = rowVersion;
    }

    public static Stock Create(
        DistributorId distributorId,
        ProductId productId,
        SaleUnit saleUnit)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del stock es requerida.");

        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto del stock es requerido.");

        return new Stock(
            StockId.New(),
            distributorId,
            productId,
            Quantity.Zero(saleUnit),
            1);
    }

    public void Increase(Quantity quantity)
    {
        EnsurePositive(quantity, "La cantidad a ingresar debe ser mayor a cero.");
        EnsureSameUnit(quantity);

        Available = Quantity.Create(
            Available.Amount + quantity.Amount,
            Available.Unit);
        RowVersion++;
    }

    public void Decrease(Quantity quantity)
    {
        EnsurePositive(quantity, "La cantidad a descontar debe ser mayor a cero.");
        EnsureSameUnit(quantity);

        if (quantity.Amount > Available.Amount)
            throw new DomainException("El stock disponible es insuficiente.");

        Available = Quantity.Create(
            Available.Amount - quantity.Amount,
            Available.Unit);
        RowVersion++;
    }

    public void Adjust(Quantity quantity)
    {
        EnsureSameUnit(quantity);
        Available = quantity;
        RowVersion++;
    }

    private void EnsureSameUnit(Quantity quantity)
    {
        if (!Available.HasSameUnitAs(quantity))
            throw new DomainException("La unidad de la cantidad no coincide con la unidad del stock.");
    }

    private static void EnsurePositive(Quantity quantity, string message)
    {
        if (quantity.Amount == 0)
            throw new DomainException(message);
    }
}
