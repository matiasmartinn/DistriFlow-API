using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Users;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

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
        OrderDateField dateField = OrderDateField.Created,
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

        if (dateField == OrderDateField.Confirmed)
        {
            query = query.Where(order => order.ConfirmedAtUtc.HasValue);
            if (dateFrom.HasValue)
                query = query.Where(order => order.ConfirmedAtUtc >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(order => order.ConfirmedAtUtc <= dateTo.Value);
        }
        else
        {
            if (dateFrom.HasValue)
                query = query.Where(order => order.CreatedAtUtc >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(order => order.CreatedAtUtc <= dateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Include(order => order.OrderLines)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int CreatedToday, int DraftCount, int ConfirmedToday)> GetSummaryAsync(
        DistributorId distributorId,
        UserId? salesRepId,
        DateTime dayFromUtc,
        DateTime dayUntilUtc,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(order => order.DistributorId == distributorId);
        if (salesRepId.HasValue)
            query = query.Where(order => order.SalesRepId == salesRepId.Value);

        var summary = await query.GroupBy(order => 1).Select(group => new
        {
            CreatedToday = group.Count(order => order.CreatedAtUtc >= dayFromUtc
                && order.CreatedAtUtc < dayUntilUtc),
            DraftCount = group.Count(order => order.Status == OrderStatus.Draft),
            ConfirmedToday = group.Count(order => order.Status == OrderStatus.Confirmed
                && order.ConfirmedAtUtc >= dayFromUtc && order.ConfirmedAtUtc < dayUntilUtc)
        }).SingleOrDefaultAsync(cancellationToken);

        return summary is null ? (0, 0, 0)
            : (summary.CreatedToday, summary.DraftCount, summary.ConfirmedToday);
    }

}
