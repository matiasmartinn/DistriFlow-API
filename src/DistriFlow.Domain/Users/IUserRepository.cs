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
        Email email,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<UserId> ids,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        Role? role,
        UserStatus? status,
        string? searchText,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}
