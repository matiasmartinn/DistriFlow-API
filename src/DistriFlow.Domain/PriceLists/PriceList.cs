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
        Name = NormalizeName(name);
    }

    public void SetPrice(ProductId productId, Money price)
    {
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
