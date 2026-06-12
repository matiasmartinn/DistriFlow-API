namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderListItemDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid SalesRepId,
    string SalesRepName,
    DateTime StartedAtUtc,
    string Status,
    decimal Total,
    int LineCount);
