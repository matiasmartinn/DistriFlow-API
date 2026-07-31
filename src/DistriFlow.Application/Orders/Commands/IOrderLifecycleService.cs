using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;

namespace DistriFlow.Application.Orders.Commands;

public interface IOrderLifecycleService
{
    Task<Result<OrderDto>> ConfirmAsync(
        Guid orderId,
        ConfirmOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> CancelAsync(
        Guid orderId,
        CancelOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}
