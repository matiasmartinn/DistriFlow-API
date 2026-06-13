using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.PriceLists.DTOs.Requests;
using DistriFlow.Application.PriceLists.DTOs.Responses;
using DistriFlow.Application.PriceLists.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;

namespace DistriFlow.Application.PriceLists;

public sealed class PriceListService : IPriceListService
{
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public PriceListService(
        IPriceListRepository priceListRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _priceListRepository = priceListRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<PriceListDto>> CreateAsync(
        CreatePriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = PriceList.Create(
                _currentUserContext.DistributorId,
                request.Name);

            await _priceListRepository.AddAsync(priceList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return priceList.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("price_list.invalid", exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<PriceListDto>>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var priceLists = includeInactive
            ? await _priceListRepository.GetAllAsync(
                _currentUserContext.DistributorId,
                cancellationToken)
            : await _priceListRepository.GetActiveAsync(
                _currentUserContext.DistributorId,
                cancellationToken);

        return priceLists.Select(priceList => priceList.ToDto()).ToList();
    }

    public async Task<Result<PriceListDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priceList = await GetPriceListAsync(id, cancellationToken);
            return priceList is null ? PriceListNotFound() : priceList.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Validation("price_list.invalid_id", exception.Message);
        }
    }

    public Task<Result> RenameAsync(
        Guid id,
        RenamePriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            priceList => priceList.Rename(request.Name),
            cancellationToken);
    }

    public async Task<Result> SetProductPriceAsync(
        Guid id,
        Guid productId,
        SetProductPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                ProductId.From(productId),
                cancellationToken);

            if (product is null || product.Status != ProductStatus.Active)
                return ProductNotFound();

            var priceList = await GetPriceListAsync(id, cancellationToken);

            if (priceList is null)
                return PriceListNotFound();

            priceList.SetPrice(product.Id, Money.Create(request.Price));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("price_list.invalid_product_price", exception.Message);
        }
    }

    public async Task<Result> RemoveProductAsync(
        Guid id,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                ProductId.From(productId),
                cancellationToken);

            if (product is null || product.Status != ProductStatus.Active)
                return ProductNotFound();

            var priceList = await GetPriceListAsync(id, cancellationToken);

            if (priceList is null)
                return PriceListNotFound();

            priceList.RemoveProduct(product.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("price_list.invalid_product", exception.Message);
        }
    }

    public Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            priceList => priceList.Deactivate(),
            cancellationToken);
    }

    public Task<Result> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            priceList => priceList.Reactivate(),
            cancellationToken);
    }

    private async Task<Result> ChangeAsync(
        Guid id,
        Action<PriceList> change,
        CancellationToken cancellationToken)
    {
        try
        {
            var priceList = await GetPriceListAsync(id, cancellationToken);

            if (priceList is null)
                return PriceListNotFound();

            change(priceList);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("price_list.invalid", exception.Message);
        }
    }

    private Task<PriceList?> GetPriceListAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _priceListRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            PriceListId.From(id),
            cancellationToken);
    }

    private static Error PriceListNotFound()
    {
        return Error.NotFound(
            "price_list.not_found",
            "Lista de precios no encontrada.");
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound(
            "product.not_found",
            "Producto no encontrado.");
    }
}
