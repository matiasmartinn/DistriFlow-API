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

    public Task<IReadOnlyList<Customer>> GetActiveAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return ListAsync(
            customer => customer.DistributorId == distributorId
                && customer.Status == CustomerStatus.Active,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetFilteredAsync(
        DistributorId distributorId,
        CustomerStatus? status,
        TaxCondition? taxCondition,
        string? zone,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(customer => customer.DistributorId == distributorId);

        if (status.HasValue)
            query = query.Where(customer => customer.Status == status.Value);

        if (taxCondition.HasValue)
            query = query.Where(customer => customer.TaxCondition == taxCondition.Value);

        if (!string.IsNullOrWhiteSpace(zone))
        {
            var normalizedZone = zone.Trim();
            query = query.Where(customer => customer.Zone == normalizedZone);
        }

        return await query
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
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
