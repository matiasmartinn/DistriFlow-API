namespace DistriFlow.Application.DistributorConfigurations.DTOs.Responses;

public sealed record DistributorConfigurationDto(
    Guid DistributorId,
    string StockPolicy,
    string PriceFallbackPolicy,
    string? OrderNumberPrefix,
    long NextOrderNumber,
    string TimeZone,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
