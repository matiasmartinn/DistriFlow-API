using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record UpdateCustomerContactRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Phone { get; init; } = null!;

    public CustomerAddressRequest? Address { get; init; }
}
