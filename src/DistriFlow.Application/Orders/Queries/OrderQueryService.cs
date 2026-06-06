using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Application.Orders.Mappers;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Orders;

namespace DistriFlow.Application.Orders.Queries;

public sealed class OrderQueryService : IOrderQueryService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public OrderQueryService(
        IOrderRepository orderRepository,
        ICurrentUserContext currentUserContext)
    {
        _orderRepository = orderRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<OrderDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                OrderId.From(id),
                cancellationToken);

            return order is null
                ? Error.NotFound("order.not_found", "Pedido no encontrado.")
                : order.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Validation("order.invalid_id", exception.Message);
        }
    }
}
