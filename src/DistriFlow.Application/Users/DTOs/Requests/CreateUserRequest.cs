using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Users.DTOs.Requests;

public sealed record CreateUserRequest
{
    [Required]
    public string Name { get; init; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = null!;

    [Required]
    public string Role { get; init; } = null!;
}
