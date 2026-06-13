using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Domain.Orders;

namespace DistriFlow.Application.Orders.Mappers;

public static class OrderMapper
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerId.Value,
            order.SalesRepId.Value,
            order.StartedAtUtc,
            order.Status.ToString(),
            order.Total.Amount,
            order.OrderLines.Select(line => line.ToDto()).ToList());
    }

    public static OrderDetailDto ToDetailDto(
        this Order order,
        string customerName,
        string salesRepName)
    {
        return new OrderDetailDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerId.Value,
            customerName,
            order.SalesRepId.Value,
            salesRepName,
            order.StartedAtUtc,
            order.Status.ToString(),
            order.Total.Amount,
            order.OrderLines.Select(line => line.ToDto()).ToList());
    }

    public static OrderListItemDto ToListItemDto(
        this Order order,
        string customerName,
        string salesRepName)
    {
        return new OrderListItemDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerId.Value,
            customerName,
            order.SalesRepId.Value,
            salesRepName,
            order.StartedAtUtc,
            order.Status.ToString(),
            order.Total.Amount,
            order.OrderLines.Count);
    }

    private static OrderLineDto ToDto(this OrderLine line)
    {
        return new OrderLineDto(
            line.ProductId.Value,
            line.ProductDescription,
            line.Quantity.Amount,
            line.Quantity.Unit.Value,
            line.UnitPrice.Amount,
            line.Subtotal.Amount);
    }
}
