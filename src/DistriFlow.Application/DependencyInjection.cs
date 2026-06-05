using DistriFlow.Application.Auth;
using DistriFlow.Application.Distributors;
using Microsoft.Extensions.DependencyInjection;

namespace DistriFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDistributorService, DistributorService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
