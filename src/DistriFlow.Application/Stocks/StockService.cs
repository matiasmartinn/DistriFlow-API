using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Stocks.DTOs.Requests;
using DistriFlow.Application.Stocks.DTOs.Responses;
using DistriFlow.Application.Stocks.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Stocks;

public sealed class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public StockService(
        IStockRepository stockRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _stockRepository = stockRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<StockDto>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await GetProductAsync(productId, cancellationToken);

            if (product is null
                || (_currentUserContext.Role == Role.SalesRep
                    && product.Status != ProductStatus.Active))
            {
                return ProductNotFound();
            }

            var stock = await _stockRepository.GetByProductIdAsync(
                _currentUserContext.DistributorId,
                product.Id,
                cancellationToken);

            return stock is null ? product.ToEmptyStockDto() : stock.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Validation("stock.invalid_product_id", exception.Message);
        }
    }

    public Task<Result<StockDto>> IncreaseAsync(
        Guid productId,
        IncreaseStockRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            productId,
            stock => stock.Increase(
                Quantity.Create(request.Amount, stock.Available.Unit)),
            cancellationToken);
    }

    public Task<Result<StockDto>> AdjustAsync(
        Guid productId,
        AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            productId,
            stock => stock.Adjust(
                Quantity.Create(request.Amount, stock.Available.Unit)),
            cancellationToken);
    }

    private async Task<Result<StockDto>> ChangeAsync(
        Guid productId,
        Action<Stock> change,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await GetProductAsync(productId, cancellationToken);

            if (product is null)
                return ProductNotFound();

            var stock = await _stockRepository.GetByProductIdAsync(
                _currentUserContext.DistributorId,
                product.Id,
                cancellationToken);

            if (stock is null)
            {
                stock = Stock.Create(
                    _currentUserContext.DistributorId,
                    product.Id,
                    product.SaleUnit);

                await _stockRepository.AddAsync(stock, cancellationToken);
            }

            change(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return stock.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("stock.invalid", exception.Message);
        }
    }

    private Task<Product?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _productRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            ProductId.From(productId),
            cancellationToken);
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound("product.not_found", "Producto no encontrado.");
    }
}
