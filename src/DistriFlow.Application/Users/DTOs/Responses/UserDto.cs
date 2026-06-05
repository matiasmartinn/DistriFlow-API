namespace DistriFlow.Application.Users.DTOs.Responses;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Status);
