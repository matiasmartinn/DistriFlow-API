using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Distributors.DTOs.Requests;

public sealed record CreateAdministratorRequest
{
    [Required]
    public string Name { get; init; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = null!;
}
