using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        DistributorId distributorId,
        CustomerId id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}
