using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.DistributorConfigurations.DTOs.Requests;
using DistriFlow.Application.DistributorConfigurations.DTOs.Responses;
using DistriFlow.Application.DistributorConfigurations.Mappers;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.DistributorConfigurations.Enums;

namespace DistriFlow.Application.DistributorConfigurations;

public sealed class DistributorConfigurationService : IDistributorConfigurationService
{
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public DistributorConfigurationService(
        IDistributorConfigurationRepository configurationRepository,
        ICurrentUserContext currentUserContext)
    {
        _configurationRepository = configurationRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<DistributorConfigurationDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationRepository.GetByDistributorIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        return configuration is null
            ? ConfigurationNotFound()
            : configuration.ToDto();
    }

    public async Task<Result<DistributorConfigurationDto>> UpdateCurrentAsync(
        UpdateDistributorConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<StockPolicy>(
                    request.StockPolicy,
                    true,
                    out var stockPolicy)
                || !Enum.IsDefined(stockPolicy))
            {
                return Error.Validation(
                    "distributor_configuration.invalid_stock_policy",
                    "La política de stock es inválida.");
            }

            if (!Enum.TryParse<PriceFallbackPolicy>(
                    request.PriceFallbackPolicy,
                    true,
                    out var priceFallbackPolicy)
                || !Enum.IsDefined(priceFallbackPolicy))
            {
                return Error.Validation(
                    "distributor_configuration.invalid_price_fallback_policy",
                    "La política de fallback de precio es inválida.");
            }

            var configuration = await _configurationRepository.GetByDistributorIdAsync(
                _currentUserContext.DistributorId,
                cancellationToken);

            if (configuration is null)
                return ConfigurationNotFound();

            configuration.Update(
                stockPolicy,
                priceFallbackPolicy,
                request.OrderNumberPrefix,
                request.NextOrderNumber,
                request.TimeZone,
                DateTime.UtcNow);

            var updated = await _configurationRepository.TryUpdateAsync(
                configuration,
                cancellationToken);

            if (!updated)
            {
                return Error.Conflict(
                    "distributor_configuration.update_conflict",
                    "La configuración fue modificada por otra operación.");
            }

            return configuration.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain(
                "distributor_configuration.invalid",
                exception.Message);
        }
    }

    private static Error ConfigurationNotFound()
    {
        return Error.NotFound(
            "distributor_configuration.not_found",
            "Configuración de distribuidora no encontrada.");
    }
}
