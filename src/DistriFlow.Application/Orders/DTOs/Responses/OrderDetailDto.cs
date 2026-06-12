namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid SalesRepId,
    string SalesRepName,
    DateTime StartedAtUtc,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);
