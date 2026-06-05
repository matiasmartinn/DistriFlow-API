using DistriFlow.Application.Auth.DTOs;
using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Auth;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
