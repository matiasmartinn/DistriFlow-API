using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Customers.DTOs.Requests;
using DistriFlow.Application.Customers.DTOs.Responses;
using DistriFlow.Application.Customers.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;

namespace DistriFlow.Application.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPriceListRepository priceListRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _customerRepository = customerRepository;
        _priceListRepository = priceListRepository;
        _productRepository = productRepository;
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
                CreateAddress(request.Address),
                priceListId.Data);

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
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(
            _currentUserContext.DistributorId,
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
            return customer is null ? CustomerNotFound() : customer.ToDto();
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
                return CustomerNotFound();

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
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            customer => customer.Deactivate(),
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
                return CustomerNotFound();

            var product = await _productRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                ProductId.From(productId),
                cancellationToken);

            if (product is null)
                return ProductNotFound();

            if (!customer.PriceListId.HasValue)
                return BasePrice(customer, product);

            var priceList = await _priceListRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                customer.PriceListId.Value,
                cancellationToken);

            if (priceList is null)
                return BasePrice(customer, product);

            var price = priceList.FindPrice(product.Id);

            return price.HasValue
                ? new EffectivePriceDto(
                    customer.Id.Value,
                    product.Id.Value,
                    price.Value.Amount,
                    "PriceList",
                    priceList.Id.Value)
                : BasePrice(customer, product);
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
                return CustomerNotFound();

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
        var priceList = await _priceListRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            priceListId,
            cancellationToken);

        return priceList is null
            ? Result<PriceListId?>.Failure(PriceListNotFound())
            : Result<PriceListId?>.Success(priceListId);
    }

    private static Address? CreateAddress(CustomerAddressRequest? request)
    {
        return request is null
            ? null
            : Address.Create(request.Street, request.CityId, request.ZipCode);
    }

    private static EffectivePriceDto BasePrice(Customer customer, Product product)
    {
        return new EffectivePriceDto(
            customer.Id.Value,
            product.Id.Value,
            product.BasePrice.Amount,
            "BasePrice",
            null);
    }

    private static Error CustomerNotFound()
    {
        return Error.NotFound("customer.not_found", "Cliente no encontrado.");
    }

    private static Error PriceListNotFound()
    {
        return Error.NotFound(
            "price_list.not_found",
            "Lista de precios no encontrada.");
    }

    private static Error ProductNotFound()
    {
        return Error.NotFound("product.not_found", "Producto no encontrado.");
    }
}
