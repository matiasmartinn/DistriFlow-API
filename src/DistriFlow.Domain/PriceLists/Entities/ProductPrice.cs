using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Products.ValueObjects;

namespace DistriFlow.Domain.PriceLists.Entities;

public sealed class ProductPrice
{
    public ProductId ProductId { get; private set; }
    public Money Price { get; private set; }

    private ProductPrice() { }

    private ProductPrice(ProductId productId, Money price)
    {
        ProductId = productId;
        Price = price;
    }

    internal static ProductPrice Create(ProductId productId, Money price)
    {
        return new ProductPrice(productId, price);
    }

    internal void ChangePrice(Money price)
    {
        Price = price;
    }
}
