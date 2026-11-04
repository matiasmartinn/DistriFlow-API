using DistriFlow.Application.Common.Notifications;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Customers;
using DistriFlow.Application.DistributorConfigurations;
using DistriFlow.Application.Distributors;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Application.Orders.Mappers;
using DistriFlow.Application.Products;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Orders.Services;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Customers.Enums;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Distributors.Enums;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.PriceLists.ValueObjects;
using DistriFlow.Domain.Products.Enums;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Orders.Commands;

public sealed class OrderDraftService : IOrderDraftService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IDistributorRepository _distributorRepository;
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly IOperationalNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly OrderIdempotency _orderIdempotency;

    public OrderDraftService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IPriceListRepository priceListRepository,
        IDistributorRepository distributorRepository,
        IDistributorConfigurationRepository configurationRepository,
        IOperationalNotifier notifier,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        OrderIdempotency orderIdempotency)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _priceListRepository = priceListRepository;
        _distributorRepository = distributorRepository;
        _configurationRepository = configurationRepository;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _orderIdempotency = orderIdempotency;
    }

    public async Task<Result<OrderDto>> StartAsync(
        StartOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!CanOperateOrders())
                return OrderErrors.OrderOperatorRequired();

            if (!TryParseOrigin(request.Origin, out var origin))
                return OrderErrors.InvalidOrderOrigin();

            var availabilityError = await GetOrderingAvailabilityErrorAsync(
                request.CustomerId,
                cancellationToken);

            if (availabilityError is not null)
                return availabilityError;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

            var idempotency = await _orderIdempotency.ReserveAsync(
                idempotencyKey,
                IdempotentOperation.StartOrder,
                null,
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

            var orderNumber = await _configurationRepository.ReserveNextOrderNumberAsync(
                _currentUserContext.DistributorId,
                DateTime.UtcNow,
                cancellationToken);

            if (orderNumber is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ConfigurationErrors.NotFound();
            }

            var order = Order.Start(
                _currentUserContext.DistributorId,
                CustomerId.From(request.CustomerId),
                _currentUserContext.UserId,
                orderNumber.Value,
                origin,
                DateTime.UtcNow);

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resourceAssigned = await _orderIdempotency.TrySetResourceIdAsync(
                idempotency.Data.Key,
                IdempotentOperation.StartOrder,
                order.Id.Value,
                cancellationToken);

            if (!resourceAssigned)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdempotencyErrors.Conflict();
            }

            await transaction.CommitAsync(cancellationToken);

            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, order.SalesRepId.Value,
                new OrderChangedNotification(order.Id.Value, order.RowVersion, order.Status.ToString()));
            return order.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("order.invalid", exception.Message);
        }
    }

    public async Task<Result<OrderDto>> AddLineAsync(
        Guid orderId,
        AddOrderLineRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orderResult = await GetEditableOrderAsync(orderId, cancellationToken);

            if (orderResult.IsFailure)
                return orderResult.Error;

            var product = await _productRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                ProductId.From(request.ProductId),
                cancellationToken);

            if (product is null || product.Status != ProductStatus.Active)
                return ProductErrors.NotFound();

            var customer = await _customerRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                orderResult.Data!.CustomerId,
                cancellationToken);

            var price = await ResolvePriceAsync(
                customer!,
                product,
                cancellationToken);

            if (price.IsFailure)
                return price.Error;

            orderResult.Data.AddLine(
                product.Id,
                product.Name,
                Quantity.Create(request.Amount, product.SaleUnit),
                price.Data!.SourceUnitPrice,
                price.Data.AppliedUnitPrice,
                price.Data.PriceOrigin,
                price.Data.PriceListId,
                price.Data.PriceListVersion,
                request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, orderResult.Data.SalesRepId.Value,
                new OrderChangedNotification(orderResult.Data.Id.Value, orderResult.Data.RowVersion, orderResult.Data.Status.ToString()));
            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderErrors.OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("order.invalid_line", exception.Message);
        }
    }

    public async Task<Result<OrderDto>> ChangeLineQuantityAsync(
        Guid orderId,
        Guid productId,
        ChangeOrderLineQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orderResult = await GetEditableOrderAsync(orderId, cancellationToken);

            if (orderResult.IsFailure)
                return orderResult.Error;

            var parsedProductId = ProductId.From(productId);
            var line = orderResult.Data!.FindLine(parsedProductId);

            if (line is null)
                return OrderErrors.OrderLineNotFound();

            orderResult.Data.ChangeLineQuantity(
                parsedProductId,
                Quantity.Create(request.Amount, line.Quantity.Unit),
                request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, orderResult.Data.SalesRepId.Value,
                new OrderChangedNotification(orderResult.Data.Id.Value, orderResult.Data.RowVersion, orderResult.Data.Status.ToString()));
            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderErrors.OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("order.invalid_line", exception.Message);
        }
    }

    public async Task<Result<OrderDto>> RemoveLineAsync(
        Guid orderId,
        Guid productId,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orderResult = await GetEditableOrderAsync(orderId, cancellationToken);

            if (orderResult.IsFailure)
                return orderResult.Error;

            var parsedProductId = ProductId.From(productId);

            if (orderResult.Data!.FindLine(parsedProductId) is null)
                return OrderErrors.OrderLineNotFound();

            orderResult.Data.RemoveLine(parsedProductId, rowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notifier.OrderChangedAsync(_currentUserContext.DistributorId.Value, orderResult.Data.SalesRepId.Value,
                new OrderChangedNotification(orderResult.Data.Id.Value, orderResult.Data.RowVersion, orderResult.Data.Status.ToString()));
            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderErrors.OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("order.invalid_line", exception.Message);
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

    private bool CanOperateOrder(Order order)
    {
        return _currentUserContext.Role == Role.Administrator
            || order.SalesRepId == _currentUserContext.UserId;
    }

    private bool CanOperateOrders()
    {
        return _currentUserContext.Role == Role.Administrator
            || _currentUserContext.Role == Role.SalesRep;
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

    private async Task<Result<OrderLinePrice>> ResolvePriceAsync(
        Customer customer,
        Product product,
        CancellationToken cancellationToken)
    {
        PriceList? priceList = null;
        if (customer.PriceListId.HasValue)
        {
            priceList = await _priceListRepository.GetEffectiveBySeriesIdAsync(
                _currentUserContext.DistributorId,
                customer.PriceListId.Value,
                DateTime.UtcNow,
                cancellationToken);
        }

        var configuration = await _configurationRepository.GetByDistributorIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (configuration is null)
            return ConfigurationErrors.NotFound();

        var pricing = OrderLinePricing.Resolve(
            priceList,
            configuration.PriceFallbackPolicy,
            product);

        return pricing.Status == OrderLinePriceStatus.Unavailable
            ? OrderErrors.PriceNotAvailable(product.Name)
            : pricing;
    }

    private static bool TryParseOrigin(string value, out OrderOrigin origin)
    {
        return Enum.TryParse(value, true, out origin)
            && Enum.IsDefined(origin);
    }
}
