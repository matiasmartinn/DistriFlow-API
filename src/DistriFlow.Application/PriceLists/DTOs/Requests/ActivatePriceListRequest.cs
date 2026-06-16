using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.PriceLists.DTOs.Requests;

public sealed record ActivatePriceListRequest
{
    [Required]
    public DateTime ValidFromUtc { get; init; }

    public DateTime? ValidUntilUtc { get; init; }
}
