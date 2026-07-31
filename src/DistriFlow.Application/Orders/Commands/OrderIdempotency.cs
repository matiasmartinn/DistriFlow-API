using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Application.Orders.Mappers;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Idempotency;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Idempotency.ValueObjects;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Orders.Commands;

public sealed class OrderIdempotency
{
    private readonly IIdempotencyRecordRepository _idempotencyRecordRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public OrderIdempotency(
        IIdempotencyRecordRepository idempotencyRecordRepository,
        IOrderRepository orderRepository,
        ICurrentUserContext currentUserContext)
    {
        _idempotencyRecordRepository = idempotencyRecordRepository;
        _orderRepository = orderRepository;
        _currentUserContext = currentUserContext;
    }

    internal async Task<Result<ReservationContext>> ReserveAsync(
        string? idempotencyKey,
        IdempotentOperation operation,
        Guid? resourceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = IdempotencyRecord.Create(
                _currentUserContext.DistributorId,
                _currentUserContext.UserId,
                idempotencyKey,
                operation,
                resourceId,
                DateTime.UtcNow);

            var reservation = await _idempotencyRecordRepository.ReserveAsync(
                record,
                cancellationToken);

            if (!reservation.Acquired
                && (reservation.CreatedByUserId != _currentUserContext.UserId
                    || reservation.Operation != operation
                    || (resourceId.HasValue
                        && reservation.ResourceId != resourceId)))
            {
                return IdempotencyErrors.Conflict();
            }

            return new ReservationContext(record.Key, reservation);
        }
        catch (DomainException exception)
        {
            return IdempotencyErrors.InvalidKey(exception.Message);
        }
    }

    internal Task<bool> TrySetResourceIdAsync(
        string key,
        IdempotentOperation operation,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return _idempotencyRecordRepository.TrySetResourceIdAsync(
            _currentUserContext.DistributorId,
            key,
            operation,
            resourceId,
            cancellationToken);
    }

    internal async Task<Result<OrderDto>> ReplayAsync(
        IdempotencyReservation reservation,
        CancellationToken cancellationToken)
    {
        if (!reservation.ResourceId.HasValue)
            return IdempotencyErrors.Conflict();

        var order = await _orderRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            OrderId.From(reservation.ResourceId.Value),
            cancellationToken);

        if (order is null || !CanOperate(order))
            return IdempotencyErrors.Conflict();

        return order.ToDto();
    }

    private bool CanOperate(Order order)
    {
        return _currentUserContext.Role == Role.Administrator
            || order.SalesRepId == _currentUserContext.UserId;
    }

    internal sealed record ReservationContext(
        string Key,
        IdempotencyReservation Reservation);
}
