using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Distributors.DTOs.Requests;

public sealed record CreateDistributorRequest
{
    [Required]
    public string Name { get; init; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Phone { get; init; } = null!;

    public AddressRequest? Address { get; init; }
}
