namespace DistriFlow.Application.Auth.DTOs;

public sealed record CurrentUserDto(
    Guid UserId,
    Guid DistributorId,
    string DistributorName,
    string Role,
    string Email,
    string Name);
