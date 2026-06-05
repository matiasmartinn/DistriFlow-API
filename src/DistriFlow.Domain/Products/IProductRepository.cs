using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        DistributorId distributorId,
        ProductId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(
        DistributorId distributorId,
        Sku sku,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}
