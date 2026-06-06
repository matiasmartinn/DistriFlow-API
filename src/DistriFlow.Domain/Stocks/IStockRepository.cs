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

    Task<bool> TryDecreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Stock stock,
        CancellationToken cancellationToken = default);
}
