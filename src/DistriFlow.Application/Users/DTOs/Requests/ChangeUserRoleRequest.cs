using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Users.DTOs.Requests;

public sealed record ChangeUserRoleRequest
{
    [Required]
    public string Role { get; init; } = null!;
}
