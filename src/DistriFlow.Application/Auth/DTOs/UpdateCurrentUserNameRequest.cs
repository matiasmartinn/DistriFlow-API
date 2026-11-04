using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Auth.DTOs;

public sealed record UpdateCurrentUserNameRequest
{
    [Required]
    public string Name { get; init; } = null!;
}
