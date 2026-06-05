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
        DistributorId distributorId,
        Email email,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            user => user.DistributorId == distributorId && user.Email == email,
            cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        DistributorId distributorId,
        Email email,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            user => user.DistributorId == distributorId && user.Email == email,
            cancellationToken);
    }
}
