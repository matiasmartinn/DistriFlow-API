using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record AddOrderLineRequest
{
    [Required]
    public Guid ProductId { get; init; }

    public decimal Amount { get; init; }

    public long RowVersion { get; init; }
}
