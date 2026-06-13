using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Stocks.DTOs.Requests;

public sealed record StockMovementListRequest
{
    public Guid? ProductId { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
