using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Users;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());

    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del usuario es requerido.");

        return new UserId(value);
    }
}
