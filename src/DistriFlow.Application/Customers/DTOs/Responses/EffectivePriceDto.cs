namespace DistriFlow.Application.Customers.DTOs.Responses;

public sealed record EffectivePriceDto(
    Guid CustomerId,
    Guid ProductId,
    decimal Price,
    string Source,
    Guid? PriceListId);
