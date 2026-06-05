namespace DistriFlow.Application.Auth.DTOs;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    Guid DistributorId,
    string Role);
