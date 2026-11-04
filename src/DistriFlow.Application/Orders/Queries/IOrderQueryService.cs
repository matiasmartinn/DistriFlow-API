using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;

namespace DistriFlow.Application.Orders.Queries;

public interface IOrderQueryService
{
    Task<Result<OrderDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<OrderListItemDto>>> GetPagedAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<OrderListItemDto>>> GetByCustomerIdAsync(
        Guid customerId,
        CustomerOrderHistoryRequest request,
        CancellationToken cancellationToken = default);
}
