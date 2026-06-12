using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Products.DTOs.Requests;

public sealed record ProductWithStockListRequest
{
    public string? Status { get; init; }
    public string? SearchText { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
