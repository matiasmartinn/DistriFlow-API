using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Distributors.DTOs.Requests;
using DistriFlow.Application.Distributors.DTOs.Responses;

namespace DistriFlow.Application.Distributors;

public interface IDistributorService
{
    Task<Result<RegisterDistributorResponse>> RegisterAsync(
        RegisterDistributorRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<DistributorDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<Result> SuspendCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<Result> ReactivateCurrentAsync(
        CancellationToken cancellationToken = default);
}
