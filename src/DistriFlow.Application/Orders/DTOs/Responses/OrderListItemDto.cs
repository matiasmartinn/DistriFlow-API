namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid SalesRepId,
    string SalesRepName,
    DateTime CreatedAtUtc,
    string Origin,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    long RowVersion,
    string Status,
    decimal Total,
    int LineCount);
