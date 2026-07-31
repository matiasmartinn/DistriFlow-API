using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Idempotency.ValueObjects;

namespace DistriFlow.Domain.Idempotency;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyReservation> ReserveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default);

    Task<bool> TrySetResourceIdAsync(
        DistributorId distributorId,
        string key,
        IdempotentOperation operation,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
