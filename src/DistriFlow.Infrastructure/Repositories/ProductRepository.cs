using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class ProductRepository
    : Repository<Product, ProductId>,
      IProductRepository
{
    public ProductRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Product?> GetByIdAsync(
        DistributorId distributorId,
        ProductId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            product => product.DistributorId == distributorId && product.Id == id,
            cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return ListAsync(
            product => product.DistributorId == distributorId,
            cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetActiveAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return ListAsync(
            product => product.DistributorId == distributorId
                && product.Status == ProductStatus.Active,
            cancellationToken);
    }

    public Task<bool> ExistsBySkuAsync(
        DistributorId distributorId,
        Sku sku,
        ProductId? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            product => product.DistributorId == distributorId
                && product.Sku == sku
                && (!excludedId.HasValue || product.Id != excludedId.Value),
            cancellationToken);
    }
}
