using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Distributors.ValueObjects;

public readonly record struct DistributorId(Guid Value)
{
    public static DistributorId New() => new(Guid.NewGuid());

    public static DistributorId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id de la distribuidora es requerido.");

        return new DistributorId(value);
    }
}
