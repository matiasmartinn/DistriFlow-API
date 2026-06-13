using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.PriceLists;

public sealed class PriceList
{
    private readonly List<ProductPrice> _productPrices = [];

    public PriceListId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public PriceListStatus Status { get; private set; }
    public IReadOnlyCollection<ProductPrice> ProductPrices => _productPrices.AsReadOnly();

    private PriceList() { }

    private PriceList(
        PriceListId id,
        DistributorId distributorId,
        string name)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Status = PriceListStatus.Active;
    }

    public static PriceList Create(DistributorId distributorId, string name)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora de la lista de precios es requerida.");

        return new PriceList(
            PriceListId.New(),
            distributorId,
            NormalizeName(name));
    }

    public void Rename(string name)
    {
        EnsureActive();
        Name = NormalizeName(name);
    }

    public void SetPrice(ProductId productId, Money price)
    {
        EnsureActive();
        EnsureProductId(productId);

        var productPrice = _productPrices.FirstOrDefault(x => x.ProductId == productId);

        if (productPrice is null)
        {
            _productPrices.Add(ProductPrice.Create(productId, price));
            return;
        }

        productPrice.ChangePrice(price);
    }

    public void RemoveProduct(ProductId productId)
    {
        EnsureActive();
        EnsureProductId(productId);

        var productPrice = _productPrices.FirstOrDefault(x => x.ProductId == productId);

        if (productPrice is not null)
            _productPrices.Remove(productPrice);
    }

    public Money? FindPrice(ProductId productId)
    {
        EnsureProductId(productId);
        return _productPrices.FirstOrDefault(x => x.ProductId == productId)?.Price;
    }

    public void Deactivate()
    {
        if (Status == PriceListStatus.Inactive)
            throw new DomainException("La lista de precios ya se encuentra inactiva.");

        Status = PriceListStatus.Inactive;
    }

    public void Reactivate()
    {
        if (Status == PriceListStatus.Active)
            throw new DomainException("La lista de precios ya se encuentra activa.");

        Status = PriceListStatus.Active;
    }

    private void EnsureActive()
    {
        if (Status != PriceListStatus.Active)
            throw new DomainException("La lista de precios se encuentra inactiva.");
    }

    private static void EnsureProductId(ProductId productId)
    {
        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto del precio es requerido.");
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la lista de precios es requerido.");

        return name.Trim();
    }
}
