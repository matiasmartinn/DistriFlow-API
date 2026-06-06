using DistriFlow.Application.Auth;
using DistriFlow.Application.Customers;
using DistriFlow.Application.Distributors;
using DistriFlow.Application.Orders.Commands;
using DistriFlow.Application.Orders.Queries;
using DistriFlow.Application.PriceLists;
using DistriFlow.Application.Products;
using DistriFlow.Application.Stocks;
using DistriFlow.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace DistriFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IDistributorService, DistributorService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrderCommandService, OrderCommandService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
