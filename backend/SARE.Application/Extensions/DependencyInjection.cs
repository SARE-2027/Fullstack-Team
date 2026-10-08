using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SARE.Application.Services;

namespace SARE.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<ProductService>();
        services.AddValidatorsFromAssemblyContaining<CategoryService>();
        return services;
    }
}
