using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record DeactivateCustomerRequest
{
    [Required]
    public string Reason { get; init; } = null!;
}
