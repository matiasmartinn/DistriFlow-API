using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.PriceLists.ValueObjects;

public readonly record struct PriceListId(Guid Value)
{
    public static PriceListId New() => new(Guid.NewGuid());

    public static PriceListId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id de la lista de precios es requerido.");

        return new PriceListId(value);
    }
}
