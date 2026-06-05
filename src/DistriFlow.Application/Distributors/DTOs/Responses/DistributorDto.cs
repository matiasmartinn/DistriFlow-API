namespace DistriFlow.Application.Distributors.DTOs.Responses;

public sealed record DistributorDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Status,
    string? Street,
    int? CityId,
    string? ZipCode);
