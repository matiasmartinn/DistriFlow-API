using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.DistributorConfigurations;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Responses;
using DistriFlow.Application.Orders.Mappers;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Orders.Queries;

public sealed class OrderQueryService : IOrderQueryService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDistributorConfigurationRepository _configurationRepository;

    public OrderQueryService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        ICurrentUserContext currentUserContext,
        IDistributorConfigurationRepository configurationRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _currentUserContext = currentUserContext;
        _configurationRepository = configurationRepository;
    }

    public async Task<Result<OrderDashboardDto>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationRepository.GetByDistributorIdAsync(
            _currentUserContext.DistributorId, cancellationToken);
        if (configuration is null)
            return ConfigurationErrors.NotFound();

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration.TimeZone);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        var dayFromUtc = OrderCalendar.StartOfDayUtc(today, timeZone);
        var dayUntilUtc = OrderCalendar.StartOfDayUtc(today.AddDays(1), timeZone);
        UserId? salesRepId = _currentUserContext.Role == Role.SalesRep ? _currentUserContext.UserId : null;
        var summary = await _orderRepository.GetSummaryAsync(
            _currentUserContext.DistributorId, salesRepId, dayFromUtc, dayUntilUtc, cancellationToken);
        var recent = await GetPagedAsync(new OrderListRequest { PageSize = 10 }, cancellationToken);
        if (recent.IsFailure)
            return recent.Error;

        return new OrderDashboardDto(today, configuration.TimeZone, dayUntilUtc,
            summary.CreatedToday, summary.DraftCount, summary.ConfirmedToday, recent.Data!.Items);
    }

    public async Task<Result<OrderDetailDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                OrderId.From(id),
                cancellationToken);

            if (order is null || !CanRead(order))
                return OrderNotFound();

            var customer = await _customerRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                order.CustomerId,
                cancellationToken);

            var salesRep = await _userRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                order.SalesRepId,
                cancellationToken);

            if (customer is null || salesRep is null)
                return OrderNotFound();

            return order.ToDetailDto(customer.Name, salesRep.Name);
        }
        catch (DomainException exception)
        {
            return Error.Validation("order.invalid_id", exception.Message);
        }
    }

    public async Task<Result<PagedResponse<OrderListItemDto>>> GetPagedAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderDateField>(request.DateField ?? "Created", true, out var dateField)
            || !Enum.IsDefined(dateField))
            return Error.Validation("order.invalid_date_field", "El tipo de fecha del pedido es inválido.");

        var dateFrom = request.DateFrom;
        var dateTo = request.DateTo;
        if (request.LocalDateFrom.HasValue || request.LocalDateTo.HasValue)
        {
            if (dateFrom.HasValue || dateTo.HasValue)
                return Error.Validation("order.invalid_date_range", "Usá fechas locales o fechas UTC, sin combinarlas.");
            if (request.LocalDateFrom > request.LocalDateTo || request.LocalDateTo == DateOnly.MaxValue)
                return Error.Validation("order.invalid_date_range", "El rango de fechas es inválido.");

            var configuration = await _configurationRepository.GetByDistributorIdAsync(
                _currentUserContext.DistributorId, cancellationToken);
            if (configuration is null)
                return ConfigurationErrors.NotFound();

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration.TimeZone);
            if (request.LocalDateFrom.HasValue)
                dateFrom = OrderCalendar.StartOfDayUtc(request.LocalDateFrom.Value, timeZone);
            if (request.LocalDateTo.HasValue)
                dateTo = OrderCalendar.StartOfDayUtc(request.LocalDateTo.Value.AddDays(1), timeZone).AddTicks(-1);
        }

        return await GetPagedAsync(
            request.Status,
            request.CustomerId,
            request.SalesRepId,
            dateFrom,
            dateTo,
            request.Page,
            request.PageSize,
            cancellationToken,
            dateField);
    }

    public async Task<Result<PagedResponse<OrderListItemDto>>> GetByCustomerIdAsync(
        Guid customerId,
        CustomerOrderHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                CustomerId.From(customerId),
                cancellationToken);

            if (customer is null)
                return CustomerNotFound();

            return await GetPagedAsync(
                request.Status,
                customer.Id.Value,
                null,
                request.DateFrom,
                request.DateTo,
                request.Page,
                request.PageSize,
                cancellationToken);
        }
        catch (DomainException exception)
        {
            return Error.Validation("customer.invalid_id", exception.Message);
        }
    }

    private async Task<Result<PagedResponse<OrderListItemDto>>> GetPagedAsync(
        string? statusValue,
        Guid? customerIdValue,
        Guid? salesRepIdValue,
        DateTime? dateFromValue,
        DateTime? dateToValue,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        OrderDateField dateField = OrderDateField.Created)
    {
        try
        {
            if (!TryParseStatus(statusValue, out var status))
            {
                return Error.Validation(
                    "order.invalid_status",
                    "El estado del pedido es inválido.");
            }

            var dateFrom = AsUtc(dateFromValue);
            var dateTo = AsUtc(dateToValue);

            if (dateFrom > dateTo)
            {
                return Error.Validation(
                    "order.invalid_date_range",
                    "La fecha desde no puede ser posterior a la fecha hasta.");
            }

            CustomerId? customerId = customerIdValue.HasValue
                ? CustomerId.From(customerIdValue.Value)
                : null;

            UserId? salesRepId = _currentUserContext.Role == Role.SalesRep
                ? _currentUserContext.UserId
                : salesRepIdValue.HasValue
                    ? UserId.From(salesRepIdValue.Value)
                    : null;

            var (orders, totalCount) = await _orderRepository.GetPagedAsync(
                _currentUserContext.DistributorId,
                status,
                customerId,
                salesRepId,
                dateFrom,
                dateTo,
                page,
                pageSize,
                dateField,
                cancellationToken);

            var customers = await _customerRepository.GetByIdsAsync(
                _currentUserContext.DistributorId,
                orders.Select(order => order.CustomerId).Distinct().ToList(),
                cancellationToken);

            var salesReps = await _userRepository.GetByIdsAsync(
                _currentUserContext.DistributorId,
                orders.Select(order => order.SalesRepId).Distinct().ToList(),
                cancellationToken);

            var customerNames = customers.ToDictionary(
                customer => customer.Id,
                customer => customer.Name);

            var salesRepNames = salesReps.ToDictionary(
                user => user.Id,
                user => user.Name);

            var items = orders.Select(order => order.ToListItemDto(
                    customerNames.GetValueOrDefault(order.CustomerId, string.Empty),
                    salesRepNames.GetValueOrDefault(order.SalesRepId, string.Empty)))
                .ToList();

            return new PagedResponse<OrderListItemDto>(
                items,
                page,
                pageSize,
                totalCount,
                TotalPages(totalCount, pageSize));
        }
        catch (DomainException exception)
        {
            return Error.Validation("order.invalid_filter", exception.Message);
        }
    }

    private bool CanRead(Order order)
    {
        return _currentUserContext.Role == Role.Administrator
            || order.SalesRepId == _currentUserContext.UserId;
    }

    private static bool TryParseStatus(string? value, out OrderStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<OrderStatus>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        if (!value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            return value;

        return value.Value.Kind == DateTimeKind.Local
            ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private static int TotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    private static Error OrderNotFound()
    {
        return Error.NotFound("order.not_found", "Pedido no encontrado.");
    }

    private static Error CustomerNotFound()
    {
        return Error.NotFound("customer.not_found", "Cliente no encontrado.");
    }
}
