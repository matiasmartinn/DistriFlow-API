using DistriFlow.Domain.Users;

namespace DistriFlow.Domain.Idempotency;

public sealed record IdempotencyReservation(
    bool Acquired,
    UserId CreatedByUserId,
    IdempotentOperation Operation,
    Guid? ResourceId);
