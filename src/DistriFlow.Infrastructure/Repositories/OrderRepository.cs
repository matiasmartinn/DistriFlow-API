using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Users;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class OrderRepository
    : Repository<Order, OrderId>,
      IOrderRepository
{
    public OrderRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Order?> GetByIdAsync(
        DistributorId distributorId,
        OrderId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(order => order.OrderLines)
            .FirstOrDefaultAsync(
                order => order.DistributorId == distributorId
                    && order.Id == id,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        OrderStatus? status,
        CustomerId? customerId,
        UserId? salesRepId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(order => order.DistributorId == distributorId);

        if (status.HasValue)
            query = query.Where(order => order.Status == status.Value);

        if (customerId.HasValue)
            query = query.Where(order => order.CustomerId == customerId.Value);

        if (salesRepId.HasValue)
            query = query.Where(order => order.SalesRepId == salesRepId.Value);

        if (dateFrom.HasValue)
            query = query.Where(order => order.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(order => order.CreatedAtUtc <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .Include(order => order.OrderLines)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

}
