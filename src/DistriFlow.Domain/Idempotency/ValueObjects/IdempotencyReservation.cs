using DistriFlow.Domain.Users;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Domain.Idempotency.ValueObjects;

public sealed record IdempotencyReservation(
    bool Acquired,
    UserId CreatedByUserId,
    IdempotentOperation Operation,
    Guid? ResourceId);
