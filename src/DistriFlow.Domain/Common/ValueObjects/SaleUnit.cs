using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record SaleUnit
{
    public string Value { get; private init; }

    private SaleUnit(string value)
    {
        Value = value;
    }

    public static SaleUnit Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("La unidad de venta es requerida.");

        return new SaleUnit(value.Trim());
    }

    public override string ToString() => Value;
}
