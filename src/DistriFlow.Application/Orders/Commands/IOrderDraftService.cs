using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;

namespace DistriFlow.Application.Orders.Commands;

public interface IOrderDraftService
{
    Task<Result<OrderDto>> StartAsync(
        StartOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> AddLineAsync(
        Guid orderId,
        AddOrderLineRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> ChangeLineQuantityAsync(
        Guid orderId,
        Guid productId,
        ChangeOrderLineQuantityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> RemoveLineAsync(
        Guid orderId,
        Guid productId,
        long rowVersion,
        CancellationToken cancellationToken = default);
}
