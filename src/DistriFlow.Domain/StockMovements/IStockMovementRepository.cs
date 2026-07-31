using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.StockMovements.Enums;

namespace DistriFlow.Domain.StockMovements;

public interface IStockMovementRepository
{
    Task<IReadOnlyList<StockMovement>> GetByReferenceAsync(
        DistributorId distributorId,
        StockMovementReferenceType referenceType,
        Guid referenceId,
        CancellationToken cancellationToken = default);

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
