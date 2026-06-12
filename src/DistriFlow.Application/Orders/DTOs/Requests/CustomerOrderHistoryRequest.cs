using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record CustomerOrderHistoryRequest
{
    public string? Status { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
