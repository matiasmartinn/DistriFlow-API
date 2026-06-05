using DistriFlow.Domain.Common;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Persistence;

public sealed class DistriFlowDbContext : DbContext, IUnitOfWork
{
    public DistriFlowDbContext(DbContextOptions<DistriFlowDbContext> options)
        : base(options) { }

    public DbSet<Distributor> Distributors => Set<Distributor>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DistriFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
