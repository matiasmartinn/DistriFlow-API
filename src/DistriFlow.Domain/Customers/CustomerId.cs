using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Customers;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());

    public static CustomerId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del cliente es requerido.");

        return new CustomerId(value);
    }
}
