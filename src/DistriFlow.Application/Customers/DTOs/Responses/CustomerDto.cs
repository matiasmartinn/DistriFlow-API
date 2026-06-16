namespace DistriFlow.Application.Customers.DTOs.Responses;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    Guid? PriceListId,
    string? Zone,
    string TaxCondition,
    string Status,
    DateTime? DeactivatedAtUtc,
    Guid? DeactivatedByUserId,
    string? DeactivationReason,
    string? Street,
    int? CityId,
    string? ZipCode);
