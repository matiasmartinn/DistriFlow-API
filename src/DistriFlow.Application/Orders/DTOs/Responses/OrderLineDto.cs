namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderLineDto(
    Guid ProductId,
    string ProductDescription,
    decimal Amount,
    string SaleUnit,
    Guid? PriceListId,
    int? PriceListVersion,
    decimal SourceUnitPrice,
    decimal AppliedUnitPrice,
    string PriceOrigin,
    decimal Subtotal);
