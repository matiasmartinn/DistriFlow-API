using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Stocks.DTOs.Requests;

public sealed record AdjustStockRequest
{
    public decimal Amount { get; init; }

    public long RowVersion { get; init; }

    [Required]
    public string Reason { get; init; } = null!;
}
