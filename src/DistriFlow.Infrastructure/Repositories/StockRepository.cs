using DistriFlow.Domain.Common.ValueObjects;
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

    public async Task<IReadOnlyList<Stock>> GetByProductIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(stock => stock.DistributorId == distributorId
                && productIds.Contains(stock.ProductId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryDecreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        bool allowNegative,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(stock => stock.DistributorId == distributorId
                && stock.ProductId == productId
                && stock.Available.Unit == quantity.Unit
                && quantity.Amount > 0);

        if (!allowNegative)
            query = query.Where(stock => stock.Available.Amount >= quantity.Amount);

        var affectedRows = await query
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    stock => stock.Available.Amount,
                    stock => stock.Available.Amount - quantity.Amount),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryIncreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbSet
            .Where(stock => stock.DistributorId == distributorId
                && stock.ProductId == productId
                && stock.Available.Unit == quantity.Unit
                && quantity.Amount > 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    stock => stock.Available.Amount,
                    stock => stock.Available.Amount + quantity.Amount),
                cancellationToken);

        return affectedRows == 1;
    }
}
