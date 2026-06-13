using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Products;

public sealed class Product
{
    public ProductId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public Sku? Sku { get; private set; }
    public SaleUnit SaleUnit { get; private set; } = null!;
    public Money BasePrice { get; private set; }
    public ProductStatus Status { get; private set; }

    private Product() { }

    private Product(
        ProductId id,
        DistributorId distributorId,
        string name,
        Sku? sku,
        SaleUnit saleUnit,
        Money basePrice)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Sku = sku;
        SaleUnit = saleUnit;
        BasePrice = basePrice;
        Status = ProductStatus.Active;
    }

    public static Product Create(
        DistributorId distributorId,
        string name,
        Sku? sku,
        SaleUnit saleUnit,
        Money basePrice)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del producto es requerida.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del producto es requerido.");

        return new Product(
            ProductId.New(),
            distributorId,
            name.Trim(),
            sku,
            saleUnit,
            basePrice);
    }

    public void ChangeBasePrice(Money basePrice)
    {
        EnsureActive();
        BasePrice = basePrice;
    }

    public void Update(
        string name,
        Sku? sku,
        SaleUnit saleUnit,
        Money basePrice)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del producto es requerido.");

        if (saleUnit != SaleUnit)
        {
            throw new DomainException(
                "La unidad de venta no puede modificarse una vez creado el producto.");
        }

        Name = name.Trim();
        Sku = sku;
        BasePrice = basePrice;
    }

    public void Discontinue()
    {
        if (Status == ProductStatus.Discontinued)
            throw new DomainException("El producto ya se encuentra discontinuado.");

        Status = ProductStatus.Discontinued;
    }

    public void Reactivate()
    {
        if (Status == ProductStatus.Active)
            throw new DomainException("El producto ya se encuentra activo.");

        Status = ProductStatus.Active;
    }

    private void EnsureActive()
    {
        if (Status != ProductStatus.Active)
            throw new DomainException("El producto se encuentra discontinuado.");
    }
}
