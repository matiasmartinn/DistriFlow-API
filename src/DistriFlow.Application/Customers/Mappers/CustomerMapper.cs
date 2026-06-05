using DistriFlow.Application.Customers.DTOs.Responses;
using DistriFlow.Domain.Customers;

namespace DistriFlow.Application.Customers.Mappers;

internal static class CustomerMapper
{
    internal static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto(
            customer.Id.Value,
            customer.Name,
            customer.Contact.Email.Value,
            customer.Contact.Phone.Value,
            customer.PriceListId?.Value,
            customer.Status.ToString(),
            customer.Address?.Street,
            customer.Address?.CityId,
            customer.Address?.ZipCode);
    }
}
