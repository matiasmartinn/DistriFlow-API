namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid SalesRepId,
    DateTime StartedAtUtc,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);
