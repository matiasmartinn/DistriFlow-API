using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.DistributorConfigurations.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Domain.DistributorConfigurations;

public interface IDistributorConfigurationRepository
{
    Task<DistributorConfiguration?> GetByDistributorIdAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<ReservedOrderNumber?> ReserveNextOrderNumberAsync(
        DistributorId distributorId,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryUpdateAsync(
        DistributorConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DistributorConfiguration configuration,
        CancellationToken cancellationToken = default);
}
