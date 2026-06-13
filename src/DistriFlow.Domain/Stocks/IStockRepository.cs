using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;

namespace DistriFlow.Domain.Stocks;

public interface IStockRepository
{
    Task<Stock?> GetByProductIdAsync(
        DistributorId distributorId,
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Stock>> GetByProductIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default);

    Task<StockChange?> TryDecreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        bool allowNegative,
        CancellationToken cancellationToken = default);

    Task<StockChange?> TryIncreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        CancellationToken cancellationToken = default);

    Task<StockChange?> TryAdjustAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity expectedQuantity,
        Quantity resultingQuantity,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Stock stock,
        CancellationToken cancellationToken = default);
}
