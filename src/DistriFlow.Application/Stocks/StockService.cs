using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Stocks.DTOs.Requests;
using DistriFlow.Application.Stocks.DTOs.Responses;
using DistriFlow.Application.Stocks.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Idempotency;
using DistriFlow.Domain.StockMovements;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Idempotency.ValueObjects;
using DistriFlow.Domain.Products.Enums;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.StockMovements.Enums;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Stocks;

public sealed class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdempotencyRecordRepository _idempotencyRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public StockService(
        IStockRepository stockRepository,
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository,
        IIdempotencyRecordRepository idempotencyRecordRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _stockRepository = stockRepository;
        _stockMovementRepository = stockMovementRepository;
        _productRepository = productRepository;
        _idempotencyRecordRepository = idempotencyRecordRepository;
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

    public async Task<Result<StockDto>> IncreaseAsync(
        Guid productId,
        IncreaseStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await GetProductAsync(productId, cancellationToken);

            if (product is null)
                return ProductNotFound();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

            var idempotency = await ReserveIdempotencyAsync(
                idempotencyKey,
                IdempotentOperation.IncreaseStock,
                product.Id.Value,
                cancellationToken);

            if (idempotency.IsFailure)
                return idempotency.Error;

            if (!idempotency.Data!.Acquired)
                return await ReplayStockAsync(product, cancellationToken);

            var quantity = Quantity.Create(request.Amount, product.SaleUnit);
            var change = await _stockRepository.TryIncreaseAsync(
                _currentUserContext.DistributorId,
                product.Id,
                quantity,
                cancellationToken);

            if (change is null)
                return StockChangeConflict();

            if (request.IsInitialLoad && change.PreviousQuantity != 0)
            {
                return Error.Domain(
                    "stock.initial_load_not_allowed",
                    "La carga inicial solo puede registrarse cuando la existencia es cero.");
            }

            var movement = StockMovement.Create(
                _currentUserContext.DistributorId,
                product.Id,
                StockMovementType.Entry,
                quantity.Amount,
                change.PreviousQuantity,
                change.ResultingQuantity,
                change.SaleUnit,
                request.IsInitialLoad
                    ? StockMovementReferenceType.InitialLoad
                    : StockMovementReferenceType.ManualEntry,
                null,
                request.Reason,
                DateTime.UtcNow,
                _currentUserContext.UserId);

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return change.ToDto(product.Id);
        }
        catch (ConcurrencyException)
        {
            return StockChangeConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("stock.invalid", exception.Message);
        }
    }

    public async Task<Result<StockDto>> AdjustAsync(
        Guid productId,
        AdjustStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await GetProductAsync(productId, cancellationToken);

            if (product is null)
                return ProductNotFound();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

            var idempotency = await ReserveIdempotencyAsync(
                idempotencyKey,
                IdempotentOperation.AdjustStock,
                product.Id.Value,
                cancellationToken);

            if (idempotency.IsFailure)
                return idempotency.Error;

            if (!idempotency.Data!.Acquired)
                return await ReplayStockAsync(product, cancellationToken);

            var stock = await _stockRepository.GetByProductIdAsync(
                _currentUserContext.DistributorId,
                product.Id,
                cancellationToken);

            if (stock is null)
                return StockNotFound();

            var resultingQuantity = Quantity.Create(
                request.Amount,
                stock.Available.Unit);

            if (resultingQuantity.Amount == stock.Available.Amount)
            {
                return Error.Domain(
                    "stock.no_change",
                    "El ajuste debe modificar la existencia actual.");
            }

            var change = await _stockRepository.TryAdjustAsync(
                _currentUserContext.DistributorId,
                product.Id,
                stock.Available,
                resultingQuantity,
                request.RowVersion,
                cancellationToken);

            if (change is null)
                return StockChangeConflict();

            var movement = StockMovement.Create(
                _currentUserContext.DistributorId,
                product.Id,
                change.ResultingQuantity > change.PreviousQuantity
                    ? StockMovementType.AdjustmentIncrease
                    : StockMovementType.AdjustmentDecrease,
                Math.Abs(change.ResultingQuantity - change.PreviousQuantity),
                change.PreviousQuantity,
                change.ResultingQuantity,
                change.SaleUnit,
                StockMovementReferenceType.ManualAdjustment,
                null,
                request.Reason,
                DateTime.UtcNow,
                _currentUserContext.UserId);

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return change.ToDto(product.Id);
        }
        catch (ConcurrencyException)
        {
            return StockChangeConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("stock.invalid", exception.Message);
        }
    }

    public async Task<Result<PagedResponse<StockMovementDto>>> GetMovementsAsync(
        StockMovementListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProductId? productId = request.ProductId.HasValue
                ? ProductId.From(request.ProductId.Value)
                : null;

            if (productId.HasValue
                && await GetProductAsync(request.ProductId!.Value, cancellationToken) is null)
            {
                return ProductNotFound();
            }

            if (!TryParseReferenceType(request.ReferenceType, out var referenceType))
            {
                return Error.Validation(
                    "stock_movement.invalid_reference_type",
                    "El tipo de referencia del movimiento es invalido.");
            }

            var dateFrom = AsUtc(request.DateFrom);
            var dateTo = AsUtc(request.DateTo);

            if (dateFrom > dateTo)
            {
                return Error.Validation(
                    "stock_movement.invalid_date_range",
                    "La fecha desde no puede ser posterior a la fecha hasta.");
            }

            var (movements, totalCount) = await _stockMovementRepository.GetPagedAsync(
                _currentUserContext.DistributorId,
                productId,
                dateFrom,
                dateTo,
                referenceType,
                request.ReferenceId,
                request.Page,
                request.PageSize,
                cancellationToken);

            return new PagedResponse<StockMovementDto>(
                movements.Select(movement => movement.ToDto()).ToList(),
                request.Page,
                request.PageSize,
                totalCount,
                TotalPages(totalCount, request.PageSize));
        }
        catch (DomainException exception)
        {
            return Error.Validation("stock_movement.invalid_filter", exception.Message);
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

    private async Task<Result<IdempotencyReservation>> ReserveIdempotencyAsync(
        string? idempotencyKey,
        IdempotentOperation operation,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = IdempotencyRecord.Create(
                _currentUserContext.DistributorId,
                _currentUserContext.UserId,
                idempotencyKey,
                operation,
                resourceId,
                DateTime.UtcNow);

            var reservation = await _idempotencyRecordRepository.ReserveAsync(
                record,
                cancellationToken);

            if (!reservation.Acquired
                && (reservation.CreatedByUserId != _currentUserContext.UserId
                    || reservation.Operation != operation
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

    private async Task<Result<StockDto>> ReplayStockAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetByProductIdAsync(
            _currentUserContext.DistributorId,
            product.Id,
            cancellationToken);

        return stock is null ? product.ToEmptyStockDto() : stock.ToDto();
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound("product.not_found", "Producto no encontrado.");
    }

    private static Error StockNotFound()
    {
        return Error.NotFound("stock.not_found", "Stock no encontrado.");
    }

    private static Error StockChangeConflict()
    {
        return Error.Conflict(
            "stock.change_conflict",
            "El stock fue modificado por otra operacion. Actualiza los datos e intenta nuevamente.");
    }

    private static Error IdempotencyConflict()
    {
        return Error.Conflict(
            "idempotency.key_conflict",
            "Idempotency-Key ya fue utilizada para otra operacion o recurso.");
    }

    private static bool TryParseReferenceType(
        string? value,
        out StockMovementReferenceType? referenceType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            referenceType = null;
            return true;
        }

        if (Enum.TryParse<StockMovementReferenceType>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            referenceType = parsed;
            return true;
        }

        referenceType = null;
        return false;
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        if (!value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            return value;

        return value.Value.Kind == DateTimeKind.Local
            ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private static int TotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
