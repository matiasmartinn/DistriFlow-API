using DistriFlow.Application.Distributors.DTOs.Responses;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Distributors.Mappers;

internal static class DistributorMapper
{
    internal static DistributorDto ToDto(this Distributor distributor)
    {
        return new DistributorDto(
            distributor.Id.Value,
            distributor.Name,
            distributor.Contact.Email.Value,
            distributor.Contact.Phone.Value,
            distributor.Status.ToString(),
            distributor.Address?.Street,
            distributor.Address?.CityId,
            distributor.Address?.ZipCode);
    }

    internal static AdministratorDto ToAdministratorDto(this User user)
    {
        return new AdministratorDto(
            user.Id.Value,
            user.DistributorId.Value,
            user.Name,
            user.Email.Value,
            user.Role.Value,
            user.Status.ToString());
    }
}
