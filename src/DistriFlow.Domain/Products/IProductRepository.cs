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

    Task<IReadOnlyList<Product>> GetActiveAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        ProductStatus? status,
        string? searchText,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(
        DistributorId distributorId,
        Sku sku,
        ProductId? excludedId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}
