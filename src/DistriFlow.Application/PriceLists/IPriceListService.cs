using DistriFlow.Application.Common.Results;
using DistriFlow.Application.PriceLists.DTOs.Requests;
using DistriFlow.Application.PriceLists.DTOs.Responses;

namespace DistriFlow.Application.PriceLists;

public interface IPriceListService
{
    Task<Result<PriceListDto>> CreateAsync(
        CreatePriceListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PriceListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PriceListDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> RenameAsync(
        Guid id,
        RenamePriceListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> SetProductPriceAsync(
        Guid id,
        Guid productId,
        SetProductPriceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveProductAsync(
        Guid id,
        Guid productId,
        CancellationToken cancellationToken = default);
}
