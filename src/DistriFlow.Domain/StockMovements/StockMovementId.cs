using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.StockMovements;

public readonly record struct StockMovementId(Guid Value)
{
    public static StockMovementId New() => new(Guid.NewGuid());

    public static StockMovementId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del movimiento de stock es requerido.");

        return new StockMovementId(value);
    }
}
