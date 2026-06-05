namespace DistriFlow.Application.Customers.DTOs.Responses;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    Guid? PriceListId,
    string Status,
    string? Street,
    int? CityId,
    string? ZipCode);
