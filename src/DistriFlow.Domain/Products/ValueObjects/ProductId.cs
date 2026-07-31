using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Products.ValueObjects;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());

    public static ProductId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del producto es requerido.");

        return new ProductId(value);
    }
}
