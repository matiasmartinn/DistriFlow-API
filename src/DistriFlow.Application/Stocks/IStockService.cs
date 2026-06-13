using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Stocks.DTOs.Requests;
using DistriFlow.Application.Stocks.DTOs.Responses;

namespace DistriFlow.Application.Stocks;

public interface IStockService
{
    Task<Result<StockDto>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Result<StockDto>> IncreaseAsync(
        Guid productId,
        IncreaseStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<StockDto>> AdjustAsync(
        Guid productId,
        AdjustStockRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<StockMovementDto>>> GetMovementsAsync(
        StockMovementListRequest request,
        CancellationToken cancellationToken = default);
}
