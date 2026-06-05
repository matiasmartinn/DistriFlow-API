using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Customers.DTOs.Requests;
using DistriFlow.Application.Customers.DTOs.Responses;

namespace DistriFlow.Application.Customers;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CustomerDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateContactAsync(
        Guid id,
        UpdateCustomerContactRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> AssignPriceListAsync(
        Guid id,
        AssignPriceListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemovePriceListAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<EffectivePriceDto>> ResolvePriceAsync(
        Guid id,
        Guid productId,
        CancellationToken cancellationToken = default);
}
