using DistriFlow.Application.Common.Notifications;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Customers;
using DistriFlow.Application.DistributorConfigurations;
using DistriFlow.Application.Distributors;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Application.Orders.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Orders.Services;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.StockMovements;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Customers.Enums;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.DistributorConfigurations.Enums;
using DistriFlow.Domain.Distributors.Enums;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Products.Enums;
using DistriFlow.Domain.StockMovements.Enums;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Orders.Commands;

public sealed class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDistributorRepository _distributorRepository;
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IOperationalNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly OrderIdempotency _orderIdempotency;

    public OrderLifecycleService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IDistributorRepository distributorRepository,
        IDistributorConfigurationRepository configurationRepository,
        IStockRepository stockRepository,
        IStockMovementRepository stockMovementRepository,
        IOperationalNotifier notifier,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        OrderIdempotency orderIdempotency)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _distributorRepository = distributorRepository;
        _configurationRepository = configurationRepository;
        _stockRepository = stockRepository;
        _stockMovementRepository = stockMovementRepository;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _orderIdempotency = orderIdempotency;
    }

    public async Task<Result<OrderDto>> ConfirmAsync(
        Guid orderId,
        ConfirmOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var idempotency = await _orderIdempotency.ReserveAsync(
                idempotencyKey,
                IdempotentOperation.ConfirmOrder,
                orderId,
                cancellationToken);

            if (idempotency.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return idempotency.Error;
            }

            if (!idempotency.Data!.Reservation.Acquired)
            {
                var replay = await _orderIdempotency.ReplayAsync(
                    idempotency.Data.Reservation,
                    cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                return replay;
            }

            var orderResult = await GetEditableOrderAsync(orderId, cancellationToken);

            if (orderResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return orderResult.Error;
            }

            var order = orderResult.Data!;
            var productAvailabilityError = await GetProductAvailabilityErrorAsync(
                order,
                cancellationToken);

            if (productAvailabilityError is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return productAvailabilityError;
            }

            var previousStatusForNotification = order.Status;
            var confirmedAtUtc = DateTime.UtcNow;
            order.Confirm(
                _currentUserContext.UserId,
                confirmedAtUtc,
                request.RowVersion);

            var configuration = await _configurationRepository.GetByDistributorIdAsync(
                _currentUserContext.DistributorId,
                cancellationToken);

            if (configuration is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ConfigurationErrors.NotFound();
            }

            var allowNegative =
                configuration.StockPolicy == StockPolicy.AllowNegativeStock;

            foreach (var line in order.OrderLines.OrderBy(
                         line => line.ProductId.Value))
            {
                var stockChange = await _stockRepository.TryDecreaseAsync(
                    _currentUserContext.DistributorId,
                    line.ProductId,
                    line.Quantity,
                    allowNegative,
                    cancellationToken);

                if (stockChange is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return OrderErrors.InsufficientStock(line.ProductDescription);
                }

                await _stockMovementRepository.AddAsync(
                    StockMovement.Create(
                        _currentUserContext.DistributorId,
                        line.ProductId,
                        StockMovementType.Exit,
                        line.Quantity.Amount,
                        stockChange.PreviousQuantity,
                        stockChange.ResultingQuantity,
                        stockChange.SaleUnit,
                        StockMovementReferenceType.Order,
                        order.Id.Value,
                        null,
                        confirmedAtUtc,
                        _currentUserContext.UserId),
                    cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, order.SalesRepId.Value,
                new OrderChangedNotification(order.Id.Value, order.RowVersion, order.Status.ToString()));
            if (order.Status == OrderStatus.Confirmed || previousStatusForNotification == OrderStatus.Confirmed)
                await _notifier.StockChangedAsync(_currentUserContext.DistributorId.Value,
                    new StockChangedNotification(order.OrderLines.Select(line => line.ProductId.Value).ToArray()));
            return order.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderErrors.OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Error.Domain("order.invalid_confirmation", exception.Message);
        }
    }

    public async Task<Result<OrderDto>> CancelAsync(
        Guid orderId,
        CancelOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var idempotency = await _orderIdempotency.ReserveAsync(
                idempotencyKey,
                IdempotentOperation.CancelOrder,
                orderId,
                cancellationToken);

            if (idempotency.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return idempotency.Error;
            }

            if (!idempotency.Data!.Reservation.Acquired)
            {
                var replay = await _orderIdempotency.ReplayAsync(
                    idempotency.Data.Reservation,
                    cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                return replay;
            }

            var order = await _orderRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                OrderId.From(orderId),
                cancellationToken);

            if (order is null || !CanOperateOrder(order))
            {
                await transaction.RollbackAsync(cancellationToken);
                return OrderErrors.OrderNotFound();
            }

            var previousStatusForNotification = order.Status;
            var previousStatus = order.Status;
            var cancelledAtUtc = DateTime.UtcNow;
            order.Cancel(
                _currentUserContext.UserId,
                cancelledAtUtc,
                request.CancellationReason,
                request.RowVersion);

            if (previousStatus == OrderStatus.Confirmed)
            {
                var exits = await _stockMovementRepository.GetByReferenceAsync(
                    _currentUserContext.DistributorId,
                    StockMovementReferenceType.Order,
                    order.Id.Value,
                    cancellationToken);

                if (!OrderStockReconciliation.MatchesConfirmation(order, exits))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return OrderErrors.StockReversalUnavailable();
                }

                foreach (var exit in exits)
                {
                    var stockChange = await _stockRepository.TryIncreaseAsync(
                        _currentUserContext.DistributorId,
                        exit.ProductId,
                        Quantity.Create(exit.Quantity, exit.SaleUnit),
                        cancellationToken);

                    if (stockChange is null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return OrderErrors.StockNotFound(
                            order.FindLine(exit.ProductId)!.ProductDescription);
                    }

                    await _stockMovementRepository.AddAsync(
                        StockMovement.Create(
                            _currentUserContext.DistributorId,
                            exit.ProductId,
                            StockMovementType.OrderCancellationReversal,
                            exit.Quantity,
                            stockChange.PreviousQuantity,
                            stockChange.ResultingQuantity,
                            stockChange.SaleUnit,
                            StockMovementReferenceType.OrderCancellation,
                            order.Id.Value,
                            order.CancellationReason,
                            cancelledAtUtc,
                            _currentUserContext.UserId),
                        cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, order.SalesRepId.Value,
                new OrderChangedNotification(order.Id.Value, order.RowVersion, order.Status.ToString()));
            if (order.Status == OrderStatus.Confirmed || previousStatusForNotification == OrderStatus.Confirmed)
                await _notifier.StockChangedAsync(_currentUserContext.DistributorId.Value,
                    new StockChangedNotification(order.OrderLines.Select(line => line.ProductId.Value).ToArray()));
            return order.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderErrors.OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Error.Domain("order.invalid_cancellation", exception.Message);
        }
    }

    private async Task<Result<Order>> GetEditableOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            OrderId.From(orderId),
            cancellationToken);

        if (order is null || !CanOperateOrder(order))
            return OrderErrors.OrderNotFound();

        var availabilityError = await GetOrderingAvailabilityErrorAsync(
            order.CustomerId.Value,
            cancellationToken);

        return availabilityError is null ? order : availabilityError;
    }

    private async Task<Error?> GetProductAvailabilityErrorAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var productIds = order.OrderLines
            .Select(line => line.ProductId)
            .ToList();

        var products = await _productRepository.GetByIdsAsync(
            _currentUserContext.DistributorId,
            productIds,
            cancellationToken);

        var activeProductIds = products
            .Where(product => product.Status == ProductStatus.Active)
            .Select(product => product.Id)
            .ToHashSet();

        var unavailableLine = order.OrderLines.FirstOrDefault(
            line => !activeProductIds.Contains(line.ProductId));

        return unavailableLine is null
            ? null
            : OrderErrors.ProductUnavailable(unavailableLine.ProductDescription);
    }

    private bool CanOperateOrder(Order order)
    {
        return _currentUserContext.Role == Role.Administrator
            || order.SalesRepId == _currentUserContext.UserId;
    }

    private async Task<Error?> GetOrderingAvailabilityErrorAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var distributor = await _distributorRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (distributor is null)
            return DistributorErrors.NotFound();

        if (distributor.Status != DistributorStatus.Active)
            return OrderErrors.DistributorSuspended();

        var customer = await _customerRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            CustomerId.From(customerId),
            cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound();

        return customer.Status == CustomerStatus.Active
            ? null
            : OrderErrors.CustomerInactive();
    }
}
