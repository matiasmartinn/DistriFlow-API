using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.PriceLists.DTOs.Requests;
using DistriFlow.Application.PriceLists.DTOs.Responses;
using DistriFlow.Application.PriceLists.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Idempotency;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Idempotency.ValueObjects;
using DistriFlow.Domain.PriceLists.ValueObjects;
using DistriFlow.Domain.Products.Enums;
using DistriFlow.Domain.Products.ValueObjects;

namespace DistriFlow.Application.PriceLists;

public sealed class PriceListService : IPriceListService
{
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdempotencyRecordRepository _idempotencyRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public PriceListService(
        IPriceListRepository priceListRepository,
        IProductRepository productRepository,
        IIdempotencyRecordRepository idempotencyRecordRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _priceListRepository = priceListRepository;
        _productRepository = productRepository;
        _idempotencyRecordRepository = idempotencyRecordRepository;
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
                request.Name,
                _currentUserContext.UserId);

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
            : await _priceListRepository.GetEffectiveAsync(
                _currentUserContext.DistributorId,
                DateTime.UtcNow,
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

    public async Task<Result<IReadOnlyList<PriceListDto>>> GetVersionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var seriesId = await GetSeriesIdAsync(id, cancellationToken);

            if (!seriesId.HasValue)
                return PriceListNotFound();

            var versions = await _priceListRepository.GetVersionsAsync(
                _currentUserContext.DistributorId,
                seriesId.Value,
                cancellationToken);

            return versions.Select(version => version.ToDto()).ToList();
        }
        catch (DomainException exception)
        {
            return Error.Validation("price_list.invalid_id", exception.Message);
        }
    }

    public async Task<Result<PriceListDto>> CreateVersionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var seriesId = await GetSeriesIdAsync(id, cancellationToken);

            if (!seriesId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PriceListNotFound();
            }

            await _priceListRepository.LockSeriesAsync(
                _currentUserContext.DistributorId,
                seriesId.Value,
                cancellationToken);

            var source = await GetPriceListAsync(id, cancellationToken);

            if (source is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PriceListNotFound();
            }

            var nextVersionNumber =
                await _priceListRepository.GetNextVersionNumberAsync(
                    _currentUserContext.DistributorId,
                    seriesId.Value,
                    cancellationToken);

            var nextVersion = source.CreateNextVersion(
                nextVersionNumber,
                _currentUserContext.UserId);

            await _priceListRepository.AddAsync(nextVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return nextVersion.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return PriceListConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Error.Domain("price_list.invalid_version", exception.Message);
        }
    }

    public Task<Result> RenameAsync(
        Guid id,
        RenamePriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            priceList => priceList.Rename(
                request.Name,
                _currentUserContext.UserId),
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

            priceList.SetPrice(
                product.Id,
                Money.Create(request.Price),
                _currentUserContext.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ConcurrencyException)
        {
            return PriceListConcurrencyConflict();
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

            if (product is null)
                return ProductNotFound();

            var priceList = await GetPriceListAsync(id, cancellationToken);

            if (priceList is null)
                return PriceListNotFound();

            priceList.RemoveProduct(product.Id, _currentUserContext.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (ConcurrencyException)
        {
            return PriceListConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("price_list.invalid_product", exception.Message);
        }
    }

    public async Task<Result<PriceListDto>> ActivateAsync(
        Guid id,
        ActivatePriceListRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var idempotency = await ReserveIdempotencyAsync(
                id,
                idempotencyKey,
                cancellationToken);

            if (idempotency.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return idempotency.Error;
            }

            if (!idempotency.Data!.Acquired)
            {
                var replay = await GetPriceListAsync(id, cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                return replay is null ? PriceListNotFound() : replay.ToDto();
            }

            var seriesId = await GetSeriesIdAsync(id, cancellationToken);

            if (!seriesId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PriceListNotFound();
            }

            await _priceListRepository.LockSeriesAsync(
                _currentUserContext.DistributorId,
                seriesId.Value,
                cancellationToken);

            var priceList = await GetPriceListAsync(id, cancellationToken);

            if (priceList is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PriceListNotFound();
            }

            priceList.Activate(
                request.ValidFromUtc,
                request.ValidUntilUtc,
                DateTime.UtcNow,
                _currentUserContext.UserId);

            var overlaps =
                await _priceListRepository.HasOverlappingActiveVersionAsync(
                    _currentUserContext.DistributorId,
                    priceList.SeriesId,
                    priceList.Id,
                    priceList.ValidFromUtc!.Value,
                    priceList.ValidUntilUtc,
                    cancellationToken);

            if (overlaps)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PriceListOverlapConflict();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return priceList.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return PriceListConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Error.Domain("price_list.invalid_activation", exception.Message);
        }
    }

    public Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            priceList => priceList.Deactivate(
                DateTime.UtcNow,
                _currentUserContext.UserId),
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
        catch (ConcurrencyException)
        {
            return PriceListConcurrencyConflict();
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

    private Task<PriceListId?> GetSeriesIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _priceListRepository.GetSeriesIdAsync(
            _currentUserContext.DistributorId,
            PriceListId.From(id),
            cancellationToken);
    }

    private async Task<Result<IdempotencyReservation>> ReserveIdempotencyAsync(
        Guid resourceId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = IdempotencyRecord.Create(
                _currentUserContext.DistributorId,
                _currentUserContext.UserId,
                idempotencyKey,
                IdempotentOperation.ActivatePriceListVersion,
                resourceId,
                DateTime.UtcNow);

            var reservation = await _idempotencyRecordRepository.ReserveAsync(
                record,
                cancellationToken);

            if (!reservation.Acquired
                && (reservation.CreatedByUserId != _currentUserContext.UserId
                    || reservation.Operation
                        != IdempotentOperation.ActivatePriceListVersion
                    || reservation.ResourceId != resourceId))
            {
                return IdempotencyConflict();
            }

            return reservation;
        }
        catch (DomainException exception)
        {
            return Error.Validation("idempotency.invalid_key", exception.Message);
        }
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

    private static Error PriceListOverlapConflict()
    {
        return Error.Conflict(
            "price_list.overlapping_active_version",
            "La vigencia se superpone con otra version activa de la lista.");
    }

    private static Error PriceListConcurrencyConflict()
    {
        return Error.Conflict(
            "price_list.concurrency_conflict",
            "La lista de precios fue modificada por otra operacion.");
    }

    private static Error IdempotencyConflict()
    {
        return Error.Conflict(
            "idempotency.key_conflict",
            "Idempotency-Key ya fue utilizada para otra operacion o recurso.");
    }
}
