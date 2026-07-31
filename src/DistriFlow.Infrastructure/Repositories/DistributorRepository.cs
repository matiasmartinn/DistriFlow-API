using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Infrastructure.Persistence;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class DistributorRepository
    : Repository<Distributor, DistributorId>,
      IDistributorRepository
{
    public DistributorRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Distributor?> GetByIdAsync(
        DistributorId id,
        CancellationToken cancellationToken = default)
    {
        return FindByIdAsync(id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            distributor => distributor.Name == name,
            cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            distributor => distributor.Contact.Email == email,
            cancellationToken);
    }
}
