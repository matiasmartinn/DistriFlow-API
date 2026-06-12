using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class UserRepository
    : Repository<User, UserId>,
      IUserRepository
{
    public UserRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<User?> GetByIdAsync(
        DistributorId distributorId,
        UserId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            user => user.DistributorId == distributorId && user.Id == id,
            cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<UserId> ids,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(user => user.DistributorId == distributorId
                && ids.Contains(user.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        DistributorId distributorId,
        Role? role,
        UserStatus? status,
        string? searchText,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(user => user.DistributorId == distributorId);

        if (role is not null)
            query = query.Where(user => user.Role == role);

        if (status.HasValue)
            query = query.Where(user => user.Status == status.Value);

        var users = await query
            .OrderBy(user => user.Name)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            users = users
                .Where(user => user.Name.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    || user.Email.Value.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return (
            users.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            users.Count);
    }
}
