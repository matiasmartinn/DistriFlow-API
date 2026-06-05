using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        DistributorId distributorId,
        UserId id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        DistributorId distributorId,
        Email email,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        DistributorId distributorId,
        Email email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}
