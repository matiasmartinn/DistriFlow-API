using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Domain.Distributors;

public interface IDistributorRepository
{
    Task<Distributor?> GetByIdAsync(
        DistributorId id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Distributor distributor,
        CancellationToken cancellationToken = default);
}
