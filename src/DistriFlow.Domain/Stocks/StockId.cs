using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Stocks;

public readonly record struct StockId(Guid Value)
{
    public static StockId New() => new(Guid.NewGuid());

    public static StockId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del stock es requerido.");

        return new StockId(value);
    }
}
