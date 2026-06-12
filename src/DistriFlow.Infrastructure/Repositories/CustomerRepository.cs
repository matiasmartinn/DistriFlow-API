using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class CustomerRepository
    : Repository<Customer, CustomerId>,
      ICustomerRepository
{
    public CustomerRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Customer?> GetByIdAsync(
        DistributorId distributorId,
        CustomerId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            customer => customer.DistributorId == distributorId
                && customer.Id == id,
            cancellationToken);
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return ListAsync(
            customer => customer.DistributorId == distributorId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetByIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<CustomerId> ids,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(customer => customer.DistributorId == distributorId
                && ids.Contains(customer.Id))
            .ToListAsync(cancellationToken);
    }
}
