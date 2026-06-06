using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
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

    public async Task<bool> TryMarkConfirmedAsync(
        DistributorId distributorId,
        OrderId id,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbSet
            .Where(order => order.DistributorId == distributorId
                && order.Id == id
                && order.Status == OrderStatus.Draft)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    order => order.Status,
                    OrderStatus.Confirmed),
                cancellationToken);

        return affectedRows == 1;
    }
}
