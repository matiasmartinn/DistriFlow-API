using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Products.DTOs.Requests;
using DistriFlow.Application.Products.DTOs.Responses;

namespace DistriFlow.Application.Products;

public interface IProductService
{
    Task<Result<ProductDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<ProductWithStockDto>>> GetWithStockAsync(
        ProductWithStockListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DiscontinueAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
