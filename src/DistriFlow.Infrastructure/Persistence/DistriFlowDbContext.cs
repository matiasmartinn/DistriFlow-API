using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DistriFlow.Infrastructure.Persistence;

public sealed class DistriFlowDbContext : DbContext, IUnitOfWork
{
    public DistriFlowDbContext(DbContextOptions<DistriFlowDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DistributorConfiguration> DistributorConfigurations =>
        Set<DistributorConfiguration>();
    public DbSet<Distributor> Distributors => Set<Distributor>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<User> Users => Set<User>();

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException(
                "El recurso fue modificado por otra operacion.",
                exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DistriFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
