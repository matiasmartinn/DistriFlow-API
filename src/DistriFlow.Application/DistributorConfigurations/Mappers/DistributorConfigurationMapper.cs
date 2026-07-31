using DistriFlow.Application.DistributorConfigurations.DTOs.Responses;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.DistributorConfigurations.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Application.DistributorConfigurations.Mappers;

internal static class DistributorConfigurationMapper
{
    internal static DistributorConfigurationDto ToDto(
        this DistributorConfiguration configuration)
    {
        return new DistributorConfigurationDto(
            configuration.DistributorId.Value,
            configuration.StockPolicy.ToString(),
            configuration.PriceFallbackPolicy.ToString(),
            configuration.OrderNumberPrefix,
            configuration.NextOrderNumber,
            configuration.TimeZone,
            configuration.CreatedAtUtc,
            configuration.UpdatedAtUtc);
    }
}
