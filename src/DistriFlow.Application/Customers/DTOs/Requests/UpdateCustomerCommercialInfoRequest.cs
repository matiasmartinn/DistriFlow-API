using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record UpdateCustomerCommercialInfoRequest
{
    public string? Zone { get; init; }

    [Required]
    public string TaxCondition { get; init; } = null!;
}
