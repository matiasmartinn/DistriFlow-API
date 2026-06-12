using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Users;

namespace DistriFlow.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        DistributorId distributorId,
        OrderId id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        OrderStatus? status,
        CustomerId? customerId,
        UserId? salesRepId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkConfirmedAsync(
        DistributorId distributorId,
        OrderId id,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkCancelledAsync(
        DistributorId distributorId,
        OrderId id,
        OrderStatus expectedStatus,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
