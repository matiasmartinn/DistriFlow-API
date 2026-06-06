using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Orders;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());

    public static OrderId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del pedido es requerido.");

        return new OrderId(value);
    }
}
