using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Products.DTOs.Requests;
using DistriFlow.Application.Products.DTOs.Responses;
using DistriFlow.Application.Products.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public ProductService(
        IProductRepository productRepository,
        IStockRepository stockRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _productRepository = productRepository;
        _stockRepository = stockRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<ProductDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sku = CreateSku(request.Sku);

            if (await SkuExistsAsync(sku, null, cancellationToken))
                return SkuConflict();

            var product = Product.Create(
                _currentUserContext.DistributorId,
                request.Name,
                sku,
                SaleUnit.Create(request.SaleUnit),
                Money.Create(request.BasePrice));

            var stock = Stock.Create(
                _currentUserContext.DistributorId,
                product.Id,
                product.SaleUnit);

            await _productRepository.AddAsync(product, cancellationToken);
            await _stockRepository.AddAsync(stock, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return product.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("product.invalid", exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetActiveAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        return products.Select(product => product.ToDto()).ToList();
    }

    public async Task<Result<PagedResponse<ProductWithStockDto>>> GetWithStockAsync(
        ProductWithStockListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(request.Status, out var requestedStatus))
        {
            return Error.Validation(
                "product.invalid_status",
                "El estado del producto es inválido.");
        }

        if (_currentUserContext.Role == Role.SalesRep
            && requestedStatus.HasValue
            && requestedStatus.Value != ProductStatus.Active)
        {
            return EmptyProductPage(request.Page, request.PageSize);
        }

        var status = _currentUserContext.Role == Role.SalesRep
            ? ProductStatus.Active
            : requestedStatus ?? ProductStatus.Active;

        var (products, totalCount) = await _productRepository.GetPagedAsync(
            _currentUserContext.DistributorId,
            status,
            request.SearchText?.Trim(),
            request.Page,
            request.PageSize,
            cancellationToken);

        var stocks = await _stockRepository.GetByProductIdsAsync(
            _currentUserContext.DistributorId,
            products.Select(product => product.Id).ToList(),
            cancellationToken);

        var stocksByProduct = stocks.ToDictionary(stock => stock.ProductId);
        var items = products.Select(product => product.ToWithStockDto(
                stocksByProduct.GetValueOrDefault(product.Id)))
            .ToList();

        return new PagedResponse<ProductWithStockDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            TotalPages(totalCount, request.PageSize));
    }

    public async Task<Result<ProductDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await GetProductAsync(id, cancellationToken);

            if (product is null
                || (_currentUserContext.Role == Role.SalesRep
                    && product.Status != ProductStatus.Active))
            {
                return ProductNotFound();
            }

            return product.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Validation("product.invalid_id", exception.Message);
        }
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await GetProductAsync(id, cancellationToken);

            if (product is null)
                return ProductNotFound();

            var sku = CreateSku(request.Sku);

            if (await SkuExistsAsync(sku, product.Id, cancellationToken))
                return SkuConflict();

            product.Update(
                request.Name,
                sku,
                SaleUnit.Create(request.SaleUnit),
                Money.Create(request.BasePrice));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("product.invalid", exception.Message);
        }
    }

    public Task<Result> DiscontinueAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            id,
            product => product.Discontinue(),
            cancellationToken);
    }

    public Task<Result> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            id,
            product => product.Reactivate(),
            cancellationToken);
    }

    private Task<Product?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _productRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            ProductId.From(id),
            cancellationToken);
    }

    private async Task<Result> ChangeStatusAsync(
        Guid id,
        Action<Product> changeStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await GetProductAsync(id, cancellationToken);

            if (product is null)
                return ProductNotFound();

            changeStatus(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("product.invalid_status", exception.Message);
        }
    }

    private Task<bool> SkuExistsAsync(
        Sku? sku,
        ProductId? excludedId,
        CancellationToken cancellationToken)
    {
        return sku is null
            ? Task.FromResult(false)
            : _productRepository.ExistsBySkuAsync(
                _currentUserContext.DistributorId,
                sku,
                excludedId,
                cancellationToken);
    }

    private static Sku? CreateSku(string? value)
    {
        return value is null ? null : Sku.Create(value);
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound(
            "product.not_found",
            "Producto no encontrado.");
    }

    private static Error SkuConflict()
    {
        return Error.Conflict(
            "product.sku_conflict",
            "Ya existe un producto con ese SKU en la distribuidora.");
    }

    private static bool TryParseStatus(string? value, out ProductStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<ProductStatus>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }

    private static PagedResponse<ProductWithStockDto> EmptyProductPage(
        int page,
        int pageSize)
    {
        return new PagedResponse<ProductWithStockDto>(
            [],
            page,
            pageSize,
            0,
            0);
    }

    private static int TotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
