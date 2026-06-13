namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid SalesRepId,
    string SalesRepName,
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
