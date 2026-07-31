using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Customers.DTOs.Requests;
using DistriFlow.Application.Customers.DTOs.Responses;
using DistriFlow.Application.Customers.Mappers;
using DistriFlow.Application.DistributorConfigurations;
using DistriFlow.Application.PriceLists;
using DistriFlow.Application.Products;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Orders.Services;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Customers.Enums;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.PriceLists.ValueObjects;
using DistriFlow.Domain.Products.Enums;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPriceListRepository priceListRepository,
        IProductRepository productRepository,
        IDistributorConfigurationRepository configurationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _customerRepository = customerRepository;
        _priceListRepository = priceListRepository;
        _productRepository = productRepository;
        _configurationRepository = configurationRepository;
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<CustomerDto>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priceListId = await GetValidPriceListIdAsync(
                request.PriceListId,
                cancellationToken);

            if (priceListId.IsFailure)
                return priceListId.Error;

            var customer = Customer.Create(
                _currentUserContext.DistributorId,
                request.Name,
                ContactInfo.Create(request.Email, request.Phone),
                ParseTaxCondition(request.TaxCondition),
                CreateAddress(request.Address),
                priceListId.Data,
                request.Zone);

            await _customerRepository.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return customer.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("customer.invalid", exception.Message);
        }
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(
        CustomerListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(request.Status, out var status))
            return CustomerErrors.InvalidStatus();

        if (!TryParseTaxCondition(request.TaxCondition, out var taxCondition))
            return CustomerErrors.InvalidTaxCondition();

        if (_currentUserContext.Role != Role.Administrator || !request.IncludeInactive)
            status = CustomerStatus.Active;

        var customers = await _customerRepository.GetFilteredAsync(
            _currentUserContext.DistributorId,
            status,
            taxCondition,
            request.Zone,
            cancellationToken);

        return customers.Select(customer => customer.ToDto()).ToList();
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await GetCustomerAsync(id, cancellationToken);
            return customer is null ? CustomerErrors.NotFound() : customer.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Validation("customer.invalid_id", exception.Message);
        }
    }

    public Task<Result> UpdateContactAsync(
        Guid id,
        UpdateCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.UpdateContact(
                ContactInfo.Create(request.Email, request.Phone),
                CreateAddress(request.Address)),
            cancellationToken);
    }

    public Task<Result> UpdateCommercialInfoAsync(
        Guid id,
        UpdateCustomerCommercialInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.UpdateCommercialInfo(
                request.Zone,
                ParseTaxCondition(request.TaxCondition)),
            cancellationToken);
    }

    public async Task<Result> AssignPriceListAsync(
        Guid id,
        AssignPriceListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priceListId = await GetValidPriceListIdAsync(
                request.PriceListId,
                cancellationToken);

            if (priceListId.IsFailure)
                return priceListId.Error;

            var customer = await GetCustomerAsync(id, cancellationToken);

            if (customer is null)
                return CustomerErrors.NotFound();

            customer.AssignPriceList(priceListId.Data!.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("customer.invalid_price_list", exception.Message);
        }
    }

    public Task<Result> RemovePriceListAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.RemovePriceList(),
            cancellationToken);
    }

    public Task<Result> DeactivateAsync(
        Guid id,
        DeactivateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.Deactivate(
                _currentUserContext.UserId,
                DateTime.UtcNow,
                request.Reason),
            cancellationToken);
    }

    public Task<Result> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.Reactivate(),
            cancellationToken);
    }

    public async Task<Result<EffectivePriceDto>> ResolvePriceAsync(
        Guid id,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await GetCustomerAsync(id, cancellationToken);

            if (customer is null)
                return CustomerErrors.NotFound();

            if (customer.Status != CustomerStatus.Active)
                return CustomerErrors.Inactive();

            var product = await _productRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                ProductId.From(productId),
                cancellationToken);

            if (product is null || product.Status != ProductStatus.Active)
                return ProductErrors.NotFound();

            PriceList? priceList = null;
            if (customer.PriceListId.HasValue)
                priceList = await _priceListRepository.GetEffectiveBySeriesIdAsync(
                    _currentUserContext.DistributorId,
                    customer.PriceListId.Value,
                    DateTime.UtcNow,
                    cancellationToken);

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
                ? CustomerErrors.PriceNotAvailable()
                : new EffectivePriceDto(
                    customer.Id.Value,
                    product.Id.Value,
                    pricing.AppliedUnitPrice.Amount,
                    pricing.PriceOrigin.ToString(),
                    pricing.PriceListId?.Value);
        }
        catch (DomainException exception)
        {
            return Error.Domain("customer.invalid_price_resolution", exception.Message);
        }
    }

    private async Task<Result> ChangeAsync(
        Guid id,
        Action<Customer> change,
        CancellationToken cancellationToken)
    {
        try
        {
            var customer = await GetCustomerAsync(id, cancellationToken);

            if (customer is null)
                return CustomerErrors.NotFound();

            change(customer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("customer.invalid", exception.Message);
        }
    }

    private Task<Customer?> GetCustomerAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _customerRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            CustomerId.From(id),
            cancellationToken);
    }

    private async Task<Result<PriceListId?>> GetValidPriceListIdAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return Result<PriceListId?>.Success(null);

        var priceListId = PriceListId.From(id.Value);
        var effectiveAtUtc = DateTime.UtcNow;
        var priceList = await _priceListRepository.GetEffectiveBySeriesIdAsync(
            _currentUserContext.DistributorId,
            priceListId,
            effectiveAtUtc,
            cancellationToken);

        if (priceList is null)
        {
            priceList = await _priceListRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                priceListId,
                cancellationToken);
        }

        return priceList is null || !priceList.IsEffectiveAt(effectiveAtUtc)
            ? Result<PriceListId?>.Failure(PriceListErrors.NotFound())
            : Result<PriceListId?>.Success(priceList.SeriesId);
    }

    private static Address? CreateAddress(CustomerAddressRequest? request)
    {
        return request is null
            ? null
            : Address.Create(request.Street, request.CityId, request.ZipCode);
    }

    private static TaxCondition ParseTaxCondition(string value)
    {
        if (Enum.TryParse<TaxCondition>(value, true, out var taxCondition)
            && Enum.IsDefined(taxCondition))
        {
            return taxCondition;
        }

        throw new DomainException("La condicion fiscal del cliente es invalida.");
    }

    private static bool TryParseTaxCondition(
        string? value,
        out TaxCondition? taxCondition)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            taxCondition = null;
            return true;
        }

        if (Enum.TryParse<TaxCondition>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            taxCondition = parsed;
            return true;
        }

        taxCondition = null;
        return false;
    }

    private static bool TryParseStatus(string? value, out CustomerStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<CustomerStatus>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }
}
