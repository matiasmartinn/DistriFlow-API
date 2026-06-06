using DistriFlow.Domain.Common;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users;
using DistriFlow.Infrastructure.Persistence;
using DistriFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistriFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DistriFlowDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DistriFlowDb es requerida.");

        services.AddDbContext<DistriFlowDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(
            provider => provider.GetRequiredService<DistriFlowDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IDistributorRepository, DistributorRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
