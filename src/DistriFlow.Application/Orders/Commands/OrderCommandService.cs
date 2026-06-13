using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
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
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Orders.Commands;

public sealed class OrderCommandService : IOrderCommandService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IDistributorRepository _distributorRepository;
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public OrderCommandService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IPriceListRepository priceListRepository,
        IDistributorRepository distributorRepository,
        IDistributorConfigurationRepository configurationRepository,
        IStockRepository stockRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _priceListRepository = priceListRepository;
        _distributorRepository = distributorRepository;
        _configurationRepository = configurationRepository;
        _stockRepository = stockRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<OrderDto>> StartAsync(
        StartOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentUserContext.Role != Role.SalesRep)
                return SalesRepRequired();

            if (!TryParseOrigin(request.Origin, out var origin))
                return InvalidOrderOrigin();

            var availabilityError = await GetOrderingAvailabilityErrorAsync(
                request.CustomerId,
                cancellationToken);

            if (availabilityError is not null)
                return availabilityError;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

            var orderNumber = await _configurationRepository.ReserveNextOrderNumberAsync(
                _currentUserContext.DistributorId,
                DateTime.UtcNow,
                cancellationToken);

            if (orderNumber is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ConfigurationNotFound();
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
            await transaction.CommitAsync(cancellationToken);

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
                return ProductNotFound();

            var customer = await _customerRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                orderResult.Data!.CustomerId,
                cancellationToken);

            var unitPrice = await ResolvePriceAsync(
                customer!,
                product,
                cancellationToken);

            if (unitPrice.IsFailure)
                return unitPrice.Error;

            orderResult.Data.AddLine(
                product.Id,
                product.Name,
                Quantity.Create(request.Amount, product.SaleUnit),
                unitPrice.Data!,
                request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderConcurrencyConflict();
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
                return OrderLineNotFound();

            orderResult.Data.ChangeLineQuantity(
                parsedProductId,
                Quantity.Create(request.Amount, line.Quantity.Unit),
                request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderConcurrencyConflict();
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
                return OrderLineNotFound();

            orderResult.Data.RemoveLine(parsedProductId, rowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return orderResult.Data.ToDto();
        }
        catch (ConcurrencyException)
        {
            return OrderConcurrencyConflict();
        }
        catch (DomainException exception)
        {
            return Error.Domain("order.invalid_line", exception.Message);
        }
    }

    public async Task<Result<OrderDto>> ConfirmAsync(
        Guid orderId,
        ConfirmOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var orderResult = await GetEditableOrderAsync(orderId, cancellationToken);

            if (orderResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return orderResult.Error;
            }

            var order = orderResult.Data!;
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
                return ConfigurationNotFound();
            }

            var allowNegative =
                configuration.StockPolicy == StockPolicy.AllowNegativeStock;

            foreach (var line in order.OrderLines.OrderBy(
                         line => line.ProductId.Value))
            {
                var decreased = await _stockRepository.TryDecreaseAsync(
                    _currentUserContext.DistributorId,
                    line.ProductId,
                    line.Quantity,
                    allowNegative,
                    cancellationToken);

                if (!decreased)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return InsufficientStock(line.ProductDescription);
                }
            }

            var markedConfirmed = await _orderRepository.TryMarkConfirmedAsync(
                _currentUserContext.DistributorId,
                order.Id,
                request.RowVersion,
                _currentUserContext.UserId,
                confirmedAtUtc,
                cancellationToken);

            if (!markedConfirmed)
            {
                await transaction.RollbackAsync(cancellationToken);
                return OrderConfirmationConflict();
            }

            await transaction.CommitAsync(cancellationToken);
            return order.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderConcurrencyConflict();
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
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var order = await _orderRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                OrderId.From(orderId),
                cancellationToken);

            if (order is null
                || order.SalesRepId != _currentUserContext.UserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return OrderNotFound();
            }

            var previousStatus = order.Status;
            var cancelledAtUtc = DateTime.UtcNow;
            order.Cancel(
                _currentUserContext.UserId,
                cancelledAtUtc,
                request.CancellationReason,
                request.RowVersion);

            if (previousStatus == OrderStatus.Confirmed)
            {
                foreach (var line in order.OrderLines.OrderBy(
                             line => line.ProductId.Value))
                {
                    var increased = await _stockRepository.TryIncreaseAsync(
                        _currentUserContext.DistributorId,
                        line.ProductId,
                        line.Quantity,
                        cancellationToken);

                    if (!increased)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return StockNotFound(line.ProductDescription);
                    }
                }
            }

            var markedCancelled = await _orderRepository.TryMarkCancelledAsync(
                _currentUserContext.DistributorId,
                order.Id,
                previousStatus,
                request.RowVersion,
                _currentUserContext.UserId,
                cancelledAtUtc,
                order.CancellationReason!,
                cancellationToken);

            if (!markedCancelled)
            {
                await transaction.RollbackAsync(cancellationToken);
                return OrderCancellationConflict();
            }

            await transaction.CommitAsync(cancellationToken);
            return order.ToDto();
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderConcurrencyConflict();
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

        if (order is null
            || order.SalesRepId != _currentUserContext.UserId)
            return OrderNotFound();

        var availabilityError = await GetOrderingAvailabilityErrorAsync(
            order.CustomerId.Value,
            cancellationToken);

        return availabilityError is null ? order : availabilityError;
    }

    private async Task<Error?> GetOrderingAvailabilityErrorAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var distributor = await _distributorRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (distributor is null)
            return DistributorNotFound();

        if (distributor.Status != DistributorStatus.Active)
        {
            return Error.Domain(
                "order.distributor_suspended",
                "No se pueden tomar pedidos para una distribuidora suspendida.");
        }

        var customer = await _customerRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            CustomerId.From(customerId),
            cancellationToken);

        if (customer is null)
            return CustomerNotFound();

        return customer.Status == CustomerStatus.Active
            ? null
            : Error.Domain(
                "order.customer_inactive",
                "No se pueden tomar pedidos para un cliente inactivo.");
    }

    private async Task<Result<Money>> ResolvePriceAsync(
        Customer customer,
        Product product,
        CancellationToken cancellationToken)
    {
        if (customer.PriceListId.HasValue)
        {
            var priceList = await _priceListRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                customer.PriceListId.Value,
                cancellationToken);

            var price = priceList?.FindPrice(product.Id);

            if (price.HasValue)
                return price.Value;
        }

        var configuration = await _configurationRepository.GetByDistributorIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (configuration is null)
            return ConfigurationNotFound();

        return configuration.PriceFallbackPolicy == PriceFallbackPolicy.UseLastValidPrice
            ? product.BasePrice
            : PriceNotAvailable(product.Name);
    }

    private static bool TryParseOrigin(string value, out OrderOrigin origin)
    {
        return Enum.TryParse(value, true, out origin)
            && Enum.IsDefined(origin);
    }

    private static Error OrderNotFound()
    {
        return Error.NotFound("order.not_found", "Pedido no encontrado.");
    }

    private static Error SalesRepRequired()
    {
        return Error.Forbidden(
            "order.sales_rep_required",
            "Solo un preventista puede operar pedidos.");
    }

    private static Error OrderLineNotFound()
    {
        return Error.NotFound(
            "order.line_not_found",
            "Línea del pedido no encontrada.");
    }

    private static Error CustomerNotFound()
    {
        return Error.NotFound("customer.not_found", "Cliente no encontrado.");
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound("product.not_found", "Producto no encontrado.");
    }

    private static Error DistributorNotFound()
    {
        return Error.NotFound(
            "distributor.not_found",
            "Distribuidora no encontrada.");
    }

    private static Error ConfigurationNotFound()
    {
        return Error.NotFound(
            "distributor_configuration.not_found",
            "Configuración de distribuidora no encontrada.");
    }

    private static Error PriceNotAvailable(string productName)
    {
        return Error.Domain(
            "order.price_not_available",
            $"No existe un precio válido para {productName}.");
    }

    private static Error InsufficientStock(string productDescription)
    {
        return Error.Domain(
            "order.insufficient_stock",
            $"El stock disponible de {productDescription} es insuficiente.");
    }

    private static Error OrderConfirmationConflict()
    {
        return Error.Conflict(
            "order.confirmation_conflict",
            "El pedido ya no se encuentra disponible para confirmar.");
    }

    private static Error OrderConcurrencyConflict()
    {
        return Error.Conflict(
            "order.concurrency_conflict",
            "El pedido fue modificado por otra operacion. Actualiza los datos e intenta nuevamente.");
    }

    private static Error InvalidOrderOrigin()
    {
        return Error.Validation(
            "order.invalid_origin",
            "El origen del pedido es invalido.");
    }

    private static Error StockNotFound(string productDescription)
    {
        return Error.Conflict(
            "order.stock_not_found",
            $"No se encontró el stock de {productDescription} para revertir el pedido.");
    }

    private static Error OrderCancellationConflict()
    {
        return Error.Conflict(
            "order.cancellation_conflict",
            "El pedido ya no se encuentra disponible para cancelar.");
    }
}
