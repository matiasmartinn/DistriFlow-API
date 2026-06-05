using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record Quantity
{
    public decimal Amount { get; private init; }
    public SaleUnit Unit { get; private init; } = null!;

    private Quantity() { }

    private Quantity(decimal amount, SaleUnit unit)
    {
        Amount = amount;
        Unit = unit;
    }

    public static Quantity Create(decimal amount, SaleUnit unit)
    {
        if (amount < 0)
            throw new DomainException("La cantidad no puede ser negativa.");

        return new Quantity(amount, unit);
    }

    public static Quantity Zero(SaleUnit unit) => new(0, unit);

    public bool HasSameUnitAs(Quantity other) => Unit == other.Unit;
}
