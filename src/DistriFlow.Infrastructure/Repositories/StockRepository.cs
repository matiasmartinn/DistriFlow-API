using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class StockRepository
    : Repository<Stock, StockId>,
      IStockRepository
{
    public StockRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Stock?> GetByProductIdAsync(
        DistributorId distributorId,
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            stock => stock.DistributorId == distributorId
                && stock.ProductId == productId,
            cancellationToken);
    }
}
