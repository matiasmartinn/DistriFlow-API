using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        DistributorId distributorId,
        CustomerId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetActiveAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetFilteredAsync(
        DistributorId distributorId,
        CustomerStatus? status,
        TaxCondition? taxCondition,
        string? zone,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetByIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<CustomerId> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
