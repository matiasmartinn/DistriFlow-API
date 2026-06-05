using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Users.DTOs.Requests;
using DistriFlow.Application.Users.DTOs.Responses;

namespace DistriFlow.Application.Users;

public interface IUserService
{
    Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ChangeRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
