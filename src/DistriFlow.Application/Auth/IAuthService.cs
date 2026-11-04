using DistriFlow.Application.Auth.DTOs;
using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Auth;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> UpdateCurrentNameAsync(
        UpdateCurrentUserNameRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ValidateCurrentSessionAsync(
        bool requireActiveDistributor,
        CancellationToken cancellationToken = default);
}
