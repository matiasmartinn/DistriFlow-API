using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class PriceListRepository
    : Repository<PriceList, PriceListId>,
      IPriceListRepository
{
    public PriceListRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<PriceList?> GetByIdAsync(
        DistributorId distributorId,
        PriceListId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(priceList => priceList.ProductPrices)
            .FirstOrDefaultAsync(
                priceList => priceList.DistributorId == distributorId
                    && priceList.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .Where(priceList => priceList.DistributorId == distributorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetActiveAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .Where(priceList => priceList.DistributorId == distributorId
                && priceList.Status == PriceListStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
