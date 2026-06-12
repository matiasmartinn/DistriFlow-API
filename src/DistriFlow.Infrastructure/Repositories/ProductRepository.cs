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

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        ProductStatus? status,
        string? searchText,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(product => product.DistributorId == distributorId);

        if (status.HasValue)
            query = query.Where(product => product.Status == status.Value);

        var products = await query
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            products = products
                .Where(product => product.Name.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    || (product.Sku is not null
                        && product.Sku.Value.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return (
            products.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            products.Count);
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
