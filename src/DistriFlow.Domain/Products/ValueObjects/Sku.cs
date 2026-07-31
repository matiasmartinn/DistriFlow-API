using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Products.ValueObjects;

public sealed record Sku
{
    public string Value { get; private init; }

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El SKU es requerido.");

        return new Sku(value.Trim());
    }

    public override string ToString() => Value;
}
