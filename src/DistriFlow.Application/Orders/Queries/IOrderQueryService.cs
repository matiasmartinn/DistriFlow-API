using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Orders.DTOs.Responses;

namespace DistriFlow.Application.Orders.Queries;

public interface IOrderQueryService
{
    Task<Result<OrderDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
