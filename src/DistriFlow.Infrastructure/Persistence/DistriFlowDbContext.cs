using DistriFlow.Domain.Common;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
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
    public DbSet<Distributor> Distributors => Set<Distributor>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DistriFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
