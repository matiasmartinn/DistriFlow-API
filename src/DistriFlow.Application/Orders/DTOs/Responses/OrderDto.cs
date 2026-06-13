namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid SalesRepId,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId,
    string Origin,
    DateTime? ConfirmedAtUtc,
    Guid? ConfirmedByUserId,
    DateTime? CancelledAtUtc,
    Guid? CancelledByUserId,
    string? CancellationReason,
    long RowVersion,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);
