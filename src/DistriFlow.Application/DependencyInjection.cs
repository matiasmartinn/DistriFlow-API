using DistriFlow.Application.Auth;
using DistriFlow.Application.Distributors;
using DistriFlow.Application.PriceLists;
using DistriFlow.Application.Products;
using DistriFlow.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace DistriFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDistributorService, DistributorService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
