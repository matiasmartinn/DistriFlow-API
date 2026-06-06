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
}
