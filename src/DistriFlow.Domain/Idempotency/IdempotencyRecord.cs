using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;

namespace DistriFlow.Domain.Idempotency;

public sealed class IdempotencyRecord
{
    public IdempotencyRecordId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public UserId CreatedByUserId { get; private set; }
    public string Key { get; private set; } = null!;
    public IdempotentOperation Operation { get; private set; }
    public Guid? ResourceId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private IdempotencyRecord() { }

    private IdempotencyRecord(
        IdempotencyRecordId id,
        DistributorId distributorId,
        UserId createdByUserId,
        string key,
        IdempotentOperation operation,
        Guid? resourceId,
        DateTime createdAtUtc)
    {
        Id = id;
        DistributorId = distributorId;
        CreatedByUserId = createdByUserId;
        Key = key;
        Operation = operation;
        ResourceId = resourceId;
        CreatedAtUtc = createdAtUtc;
    }

    public static IdempotencyRecord Create(
        DistributorId distributorId,
        UserId createdByUserId,
        string? key,
        IdempotentOperation operation,
        Guid? resourceId,
        DateTime createdAtUtc)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora es requerida.");

        if (createdByUserId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable es requerido.");

        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("El encabezado Idempotency-Key es requerido.");

        var normalizedKey = key.Trim();

        if (normalizedKey.Length > 100)
            throw new DomainException("Idempotency-Key no puede superar 100 caracteres.");

        if (!Enum.IsDefined(operation))
            throw new DomainException("La operacion idempotente es invalida.");

        if (resourceId == Guid.Empty)
            throw new DomainException("La referencia del recurso es invalida.");

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha debe estar expresada en UTC.");

        return new IdempotencyRecord(
            IdempotencyRecordId.New(),
            distributorId,
            createdByUserId,
            normalizedKey,
            operation,
            resourceId,
            createdAtUtc);
    }
}
