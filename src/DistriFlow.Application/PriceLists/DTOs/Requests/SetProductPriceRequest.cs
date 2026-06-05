namespace DistriFlow.Application.PriceLists.DTOs.Requests;

public sealed record SetProductPriceRequest
{
    public decimal Price { get; init; }
}
