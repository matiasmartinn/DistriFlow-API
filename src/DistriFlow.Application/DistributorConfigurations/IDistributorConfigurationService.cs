using DistriFlow.Application.Common.Results;
using DistriFlow.Application.DistributorConfigurations.DTOs.Requests;
using DistriFlow.Application.DistributorConfigurations.DTOs.Responses;

namespace DistriFlow.Application.DistributorConfigurations;

public interface IDistributorConfigurationService
{
    Task<Result<DistributorConfigurationDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<Result<DistributorConfigurationDto>> UpdateCurrentAsync(
        UpdateDistributorConfigurationRequest request,
        CancellationToken cancellationToken = default);
}
