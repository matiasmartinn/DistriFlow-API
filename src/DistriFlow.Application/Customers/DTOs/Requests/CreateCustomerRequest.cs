using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record CreateCustomerRequest
{
    [Required]
    public string Name { get; init; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Phone { get; init; } = null!;

    public CustomerAddressRequest? Address { get; init; }

    public Guid? PriceListId { get; init; }

    public string? Zone { get; init; }

    [Required]
    public string TaxCondition { get; init; } = null!;
}
