using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Products.DTOs.Requests;

public sealed record CreateProductRequest
{
    [Required]
    public string Name { get; init; } = null!;

    public string? Sku { get; init; }

    [Required]
    public string SaleUnit { get; init; } = null!;

    public decimal BasePrice { get; init; }
}
