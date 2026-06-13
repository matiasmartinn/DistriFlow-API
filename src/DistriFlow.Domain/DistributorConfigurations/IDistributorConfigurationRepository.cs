using DistriFlow.Domain.Distributors;

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
