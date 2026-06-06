using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        DistributorId distributorId,
        OrderId id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
