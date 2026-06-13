using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record StartOrderRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    public string Origin { get; init; } = "Online";
}
