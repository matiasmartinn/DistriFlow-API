using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Idempotency;

public readonly record struct IdempotencyRecordId(Guid Value)
{
    public static IdempotencyRecordId New() => new(Guid.NewGuid());

    public static IdempotencyRecordId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("El id del registro de idempotencia es requerido.");

        return new IdempotencyRecordId(value);
    }
}
