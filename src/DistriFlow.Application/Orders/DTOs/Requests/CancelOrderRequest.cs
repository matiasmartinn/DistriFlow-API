using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record CancelOrderRequest
{
    [Required]
    public string CancellationReason { get; init; } = null!;

    public long RowVersion { get; init; }
}
