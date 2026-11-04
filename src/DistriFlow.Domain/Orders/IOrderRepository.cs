using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

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
        OrderDateField dateField = OrderDateField.Created,
        CancellationToken cancellationToken = default);

    Task<(int CreatedToday, int DraftCount, int ConfirmedToday)> GetSummaryAsync(
        DistributorId distributorId,
        UserId? salesRepId,
        DateTime dayFromUtc,
        DateTime dayUntilUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
