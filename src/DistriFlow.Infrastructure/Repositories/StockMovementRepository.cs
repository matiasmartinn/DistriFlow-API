using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.StockMovements;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class StockMovementRepository
    : Repository<StockMovement, StockMovementId>,
      IStockMovementRepository
{
    public StockMovementRepository(DistriFlowDbContext context)
        : base(context) { }

    public async Task<IReadOnlyList<StockMovement>> GetByReferenceAsync(
        DistributorId distributorId,
        StockMovementReferenceType referenceType,
        Guid referenceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(movement => movement.DistributorId == distributorId
                && movement.ReferenceType == referenceType
                && movement.ReferenceId == referenceId)
            .OrderBy(movement => movement.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        ProductId? productId,
        DateTime? dateFrom,
        DateTime? dateTo,
        StockMovementReferenceType? referenceType,
        Guid? referenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(movement => movement.DistributorId == distributorId);

        if (productId.HasValue)
            query = query.Where(movement => movement.ProductId == productId.Value);

        if (dateFrom.HasValue)
            query = query.Where(movement => movement.CreatedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(movement => movement.CreatedAtUtc <= dateTo.Value);

        if (referenceType.HasValue)
            query = query.Where(movement => movement.ReferenceType == referenceType.Value);

        if (referenceId.HasValue)
            query = query.Where(movement => movement.ReferenceId == referenceId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
