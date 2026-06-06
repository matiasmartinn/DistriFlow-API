namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderLineDto(
    Guid ProductId,
    string ProductDescription,
    decimal Amount,
    string SaleUnit,
    decimal UnitPrice,
    decimal Subtotal);
