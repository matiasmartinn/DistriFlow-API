using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.PriceLists.DTOs.Requests;

public sealed record CreatePriceListRequest
{
    [Required]
    public string Name { get; init; } = null!;
}
