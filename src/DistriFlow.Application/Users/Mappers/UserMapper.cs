using DistriFlow.Application.Users.DTOs.Responses;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Users.Mappers;

internal static class UserMapper
{
    internal static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id.Value,
            user.Name,
            user.Email.Value,
            user.Role.Value,
            user.Status.ToString());
    }
}
