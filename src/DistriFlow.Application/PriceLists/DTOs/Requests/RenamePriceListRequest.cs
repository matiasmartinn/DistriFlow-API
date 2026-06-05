using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.PriceLists.DTOs.Requests;

public sealed record RenamePriceListRequest
{
    [Required]
    public string Name { get; init; } = null!;
}
