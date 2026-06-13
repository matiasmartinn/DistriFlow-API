using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Users.DTOs.Requests;

public sealed record DeactivateUserRequest
{
    [Required]
    public string Reason { get; init; } = null!;
}
