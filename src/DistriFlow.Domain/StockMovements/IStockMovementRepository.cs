using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.StockMovements;

public interface IStockMovementRepository
{
    Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        ProductId? productId,
        DateTime? dateFrom,
        DateTime? dateTo,
        StockMovementReferenceType? referenceType,
        Guid? referenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default);
}
