namespace DistriFlow.Application.Distributors.DTOs.Responses;

public sealed record AdministratorDto(
    Guid Id,
    Guid DistributorId,
    string Name,
    string Email,
    string Role,
    string Status);
